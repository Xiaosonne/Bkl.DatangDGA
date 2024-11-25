using Bkl.Dst.Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Bkl.Infrastructure;

namespace Bkl.Dst.Grains
{
    public class StreamGrain : Grain, IStreamGrain
    //, INotificationPublisher
    {
        public class State
        {
            public Guid StreamId { get; set; }
            public string NS { get; internal set; }
        }

        private ILogger<StreamGrain> _logger;
        private IPersistentState<State> _state;
        private IAsyncStream<SrClientMessage> _stream;
        public StreamGrain([PersistentState("signalrState", BklConstants.RedisProvider)] IPersistentState<State> state,
            ILogger<StreamGrain> logger)

        {
            _logger = logger;
            _state = state;
        }

        public override async Task OnActivateAsync()
        {
            await _state.ReadStateAsync();
            if (!_state.RecordExists)
            {
                _state.State = new State
                {
                    NS = "ns-" + this.GetPrimaryKeyString(),
                    StreamId = new Guid(this.GetPrimaryKeyString().Get32MD5())
                };
            }
            _stream = base.GetStreamProvider(BklConstants.StreamProvider).GetStream<SrClientMessage>(_state.State.StreamId, _state.State.NS);
            await _state.WriteStateAsync();
            await base.OnActivateAsync();
        }

        public async Task SendMessage(SrClientMessage msg)
        {
            try
            {
                await _stream.OnNextAsync(msg);
            }
            catch (Exception ex)
            {
                _logger.LogError("SendMessageError:" + ex.ToString());
            }
        }

        public Task<StreamResponse> GetStreamInfo()
        {
            return Task.FromResult(new StreamResponse { StreamNamespace = _state.State.NS, StreamId = _state.State.StreamId });
        }
    }
}
