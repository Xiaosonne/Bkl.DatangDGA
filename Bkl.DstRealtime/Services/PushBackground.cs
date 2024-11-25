using Orleans;
using System;
using System.Threading.Tasks;
using Bkl.Dst.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Orleans.Streams;
using Microsoft.Extensions.Hosting;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Collections.Generic;
using Bkl.Infrastructure;
using System.Linq;
using Bkl.DstRealtime.Hubs;

namespace Bkl.DstRealtime.Services
{

    public interface IPushOption<Thub>
    {
        string MessageType { get; }
        string ClientMethod { get; }
        string StreamGrainId { get; }
    }
    public class PushStateOption : IPushOption<PushStateHub>
    {
        public string MessageType => "state";

        public string ClientMethod => "onStateWithMeta";

        public string StreamGrainId => "signalrStateStream";
    }
    public class PushAlarmOption : IPushOption<PushAlarmHub>
    {
        public string MessageType => "alarm";

        public string ClientMethod => "onAlarmWithMeta";

        public string StreamGrainId => "signalrAlarmStream";
    }
    public interface IPushBackground<THub> where THub : Hub
    {

    }


    public class PushBackground<THub> : BackgroundService, IAsyncObserver<SrClientMessage>, IPushBackground<THub>
        where THub : Hub
    {
        private ILogger<PushBackground<THub>> _logger;
        private IServiceProvider _serviceProvider;
        private IServiceScope _scope;
        private IStreamGrain groupStateGrain;
        private IAsyncStream<SrClientMessage> _stream;
        private StreamSubscriptionHandle<SrClientMessage> _substate;
        private IHubContext<THub> _hubContext;
        private IRedisClient _redis;
        private IPushOption<THub> _option;
        private IBackgroundTaskQueue<SrClientMessage> _messageQueue;

        public PushBackground(IServiceProvider serviceProvider, IPushOption<THub> option, ILogger<PushBackground<THub>> logger)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _scope = _serviceProvider.CreateScope();
            _redis = _scope.ServiceProvider.GetService<IRedisClient>();
            _option = option;
            _messageQueue = _scope.ServiceProvider.GetService<IBackgroundTaskQueue<SrClientMessage>>();
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _task = ExecuteAsync(cancellationToken);
            return Task.CompletedTask;
        }
        IClusterClient _clusterClient = null;
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _hubContext = _serviceProvider.GetService<IHubContext<THub>>();
            while (!stoppingToken.IsCancellationRequested)
            {
                if ((_clusterClient == null || !_clusterClient.IsInitialized))
                {
                    try
                    {
                        _clusterClient = _serviceProvider.GetService<IClusterClient>();
                    }
                    catch (Exception ex)
                    {
                        _clusterClient = null;
                        _logger.LogError("cluster client initial error " + ex.ToString());
                    }
                    await Task.Delay(1000);
                    continue;
                }

                await SubscribeStreamOnDeactivate();

                var item = await _messageQueue.DequeueAsync(stoppingToken);
                if (item == null)
                {
                    _logger.LogInformation("Data Dequeue Null");
                    await Task.Delay(1000);
                    continue;
                }
                _lastActive = DateTime.Now;

                var subinfo = _redis.GetRangeFromSortedSetByHighestScore("Orleans.Sublist", item.DeviceId, item.DeviceId);

                foreach (var sub in subinfo.Select(s => SrJoinGroupRequest.Parse(s)).GroupBy(s => s.GroupId))
                {
                    if (item.MessageType == _option.MessageType && sub.Count(s => s.ClientMethod == _option.ClientMethod) > 0)
                    {
                        var dic = JsonSerializer.Deserialize<Dictionary<string, object>>(item.Data);
                        await _hubContext.Clients.Group(sub.Key).SendAsync(_option.ClientMethod, dic);
                    }
                }
            }


        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_substate != null)
                await _substate.UnsubscribeAsync();
            _logger.LogInformation("stopAsync _streamSub unsubscribe");
        }

        DateTime _lastActive = DateTime.MinValue;
        private Task _task;

        public async Task OnNextAsync(SrClientMessage item, StreamSequenceToken token = null)
        {
            _lastActive = DateTime.Now;
            await _messageQueue.EnqueueAsync(item);

        }

        public Task OnCompletedAsync()
        {
            _logger.LogInformation("stream completed");
            return Task.CompletedTask;
        }

        public Task OnErrorAsync(Exception ex)
        {
            _logger.LogError(ex.ToString());
            return Task.CompletedTask;
        }

        async Task SubscribeStreamOnDeactivate()
        {


            if (DateTime.Now.Subtract(_lastActive).TotalMinutes > 1)
            {
                try
                {
                    if (_substate != null)
                    {
                        await _substate.UnsubscribeAsync();
                        _logger.LogWarning($"UnsubscribeAsync _substate {_substate.StreamIdentity}");
                    }
                }
                catch (Exception ex)
                {
                    _substate = null;
                    _logger.LogError(ex.ToString());
                }

                try
                {
                    groupStateGrain = _clusterClient.GetGrain<IStreamGrain>(_option.StreamGrainId);
                    var respState = await groupStateGrain.GetStreamInfo();

                    var streamProvider = _clusterClient.GetStreamProvider(BklConstants.StreamProvider);
                    _stream = streamProvider.GetStream<SrClientMessage>(respState.StreamId, respState.StreamNamespace);  
                    _substate = await _stream.SubscribeAsync(this);
                    _logger.LogInformation("ExecuteAsync _streamSub subscribe");
                    _lastActive = DateTime.Now;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                }
            }
        }

    }

}
