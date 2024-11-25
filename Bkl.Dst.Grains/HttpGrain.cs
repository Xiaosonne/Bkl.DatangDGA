using Bkl.Dst.Interfaces;
using Bkl.Infrastructure;
using Bkl.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bkl.Dst.Grains
{
    public class HttpGrain : Grain, IHttpGain
    {
        private ILogger<HttpGrain> _logger;
        private IPersistentState<State> _state1;

        public class State
        {
            public HttpPushPollConfig Config { get; set; }
            public DateTime LastRead { get; set; }
        }
        public HttpGrain([PersistentState("modbusConnection", BklConstants.RedisProvider)] IPersistentState<State> state,
          ILogger<HttpGrain> logger) : base()
        {
            _logger = logger;
            _state1 = state;
        }
        HttpClient httpClient = new HttpClient(new HttpClientHandler());
        public override async Task OnActivateAsync()
        {
            string key = this.GetPrimaryKeyString();
            var redisclient = this.ServiceProvider.GetService<IRedisClient>();
            if (!_state1.RecordExists || DateTime.Now.Subtract(_state1.State.LastRead).TotalMinutes > 5)
            {
                string config = redisclient.Get($"HttpConfig:{key}");
                HttpPushPollConfig httpConfig = JsonSerializer.Deserialize<HttpPushPollConfig>(config);
                _state1.State = new State { LastRead = DateTime.Now, Config = httpConfig };
                await this._state1.WriteStateAsync();
            }

            await this.RegisterOrUpdateReminder("httpWeakUp", TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1));
        }
        public async Task ReceiveReminder(string reminderName, TickStatus status)
        {
            var config = this._state1.State.Config;
            switch (config.Action)
            {
                //推送 设备状态
                case "push":
                    HttpContent content;
                    HttpResponseMessage resp=null;
                    switch (config.Method)
                    {
                        case "POST":
                            var device = this.GrainFactory.GetGrain<IDeviceGrain>(new DeviceGrainId(config.DeviceId));
                            content = new StringContent(JsonSerializer.Serialize(new
                            {
                                Status = await device.GetStatus(),
                                Alarm = await device.GetAlarms(),
                            }));
                            foreach (var hd in config.Headers)
                            {
                                content.Headers.Add(hd.Key, hd.Value);
                            }
                            resp = await httpClient.PostAsync(config.Url, content);
                            break; 
                        default:
                            _logger.LogError($"unkonws config {config.Url} {config.Action} {config.Method} {config.DeviceId}");
                            break;
                    }
                    if (resp != null)
                    {
                        _logger.LogInformation($"config {config.Url} {config.Action} {config.Method} {config.DeviceId} {resp.StatusCode} {await resp.Content.ReadAsStringAsync()}");
                    }
                    break;

                //拉取 设备状态
                case "pull":
                    switch (config.Method)
                    { 
                        case "GET":
                            break;
                        default:
                            _logger.LogError($"unkonws config {config.Url} {config.Action} {config.Method} {config.DeviceId}");
                            break;
                    }
                    break;
                default:
                    break;
            }
        }
        public Task Weakup()
        {
            return Task.CompletedTask;
        }
    }
}
