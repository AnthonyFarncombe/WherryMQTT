using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WherryMQTT.Hubs;

namespace WherryMQTT.Services
{
    public class MqttService : IHostedService
    {
        private readonly ILogger<MqttService> _logger;
        private readonly IHubContext<MqttHub> _hubContext;
        private readonly MqttFactory _factory;
        private readonly IMqttClient _mqttClient;
        private readonly IMqttClientOptions _options;

        public MqttService(ILogger<MqttService> logger, IConfiguration config, IHubContext<MqttHub> hubContext)
        {
            _logger = logger;
            _hubContext = hubContext;
            _factory = new MqttFactory();
            _mqttClient = _factory.CreateMqttClient();
            _options = new MqttClientOptionsBuilder()
                .WithTcpServer(config.GetValue<string>("MqttBroker"))
                .Build();

            _mqttClient.UseApplicationMessageReceivedHandler(async e =>
            {
                _logger.LogInformation("### RECEIVED APPLICATION MESSAGE ####");
                _logger.LogInformation($"+ Topic = {e.ApplicationMessage.Topic}");
                _logger.LogInformation($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
                _logger.LogInformation($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                _logger.LogInformation($"+ Retain = {e.ApplicationMessage.Retain}");

                await _hubContext.Clients.All.SendAsync("NewMessage", e.ApplicationMessage.Topic, Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
            });

            _mqttClient.UseConnectedHandler(async e =>
            {
                _logger.LogInformation("### CONNECTED WITH SERVER ###");

                await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("#").Build());

                _logger.LogInformation("### SUBSCRIBED ###");
            });
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return _mqttClient.ConnectAsync(_options, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _mqttClient.DisconnectAsync(null, cancellationToken);
        }
    }
}
