using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Packets;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace UnifiedNamespaceLib.Services;

public class MqttClientService([ServiceKey] string serviceKey, IConfiguration config, IServiceProvider sp, ILogger<MqttClientService> logger) : IMessagingService
{
    MqttClientFactory mqttFactory;
    IMqttClient mqttClient;

    List<Action<MqttApplicationMessage>> handlers = new();
    List<MqttTopicFilter> topicFilters;
    private MqttClientConnectResult connectResponse;

    public async Task<MqttClientService> ConnectAsync(string clientId = null)
    {
        var stay = true;
        while (stay)
        {
            try
            {
                await ConnectImplAsync(clientId);
                stay = false;
            }
            catch (Exception ex)
            {
                logger.LogWarning($"MqttClient {clientId} failed...then retry");
            }
        }
        return this;
    }

    private async Task<MqttClientService> ConnectImplAsync(string clientId = null)
    {
        this.mqttFactory = new MqttClientFactory();
        this.mqttClient = mqttFactory.CreateMqttClient();

        var username = config[$"{serviceKey}:username"];
        var password = config[$"{serviceKey}:password"];
        clientId ??= username;
        var host = config[$"{serviceKey}:host"];
        var port = int.Parse(config[$"{serviceKey}:port"]);

        var mqttClientOptionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithClientId(clientId)
            .WithCredentials(username, password)
            .WithTimeout(TimeSpan.FromSeconds(60));

        var pemKey = X509Certificate2.CreateFromPemFile(
            config[$"{serviceKey}:certificate:pem"],
            config[$"{serviceKey}:certificate:key"]
        );
        var pkcs12 = pemKey.Export(X509ContentType.Pkcs12);
        var certificate = new X509Certificate2(pkcs12);

        mqttClientOptionsBuilder = mqttClientOptionsBuilder
            .WithTlsOptions(configure =>
                configure
                    .UseTls()
                    .WithClientCertificates([certificate])
        );
        var mqttClientOptions = mqttClientOptionsBuilder.Build();

        this.connectResponse = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        mqttClient.DisconnectedAsync += async (s) =>
        {
        };

        mqttClient.ApplicationMessageReceivedAsync += (s) =>
        {
            foreach (var handler in handlers)
            {
                try
                {
                    handler(s.ApplicationMessage);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"MqttClient {connectResponse.AssignedClientIdentifier}: {ex.Message}");
                }
            }
            return Task.CompletedTask;
        };

        return this;
    }

    async Task<MqttClientService> DisconnectAsync()
    {
        // Send a clean disconnect to the server by calling _DisconnectAsync_. Without this the TCP connection
        // gets dropped and the server will handle this as a non clean disconnect (see MQTT spec for details).
        var mqttClientDisconnectOptions = mqttFactory.CreateClientDisconnectOptionsBuilder().Build();

        await mqttClient.DisconnectAsync(mqttClientDisconnectOptions, CancellationToken.None);

        return this;
    }

    public async Task<MqttClientService> PublishAsync<TPayload>(string topic, TPayload payload, bool retainFlag = true)
    {
        if (mqttClient is null) throw new InvalidOperationException("mqttClient is null");
        if (!mqttClient.IsConnected) throw new InvalidOperationException("!mqttClient.IsConnected");

        try
        {
            var applicationMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(topic)
                        .WithPayload(JsonSerializer.Serialize(payload))
                        //.WithRetainFlag(retainFlag)
                        .Build();

            var result = await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);
        }
        catch (Exception ex)
        {
            throw ex;
        }

        return this;
    }

    public MqttClientService Handle(Action<MqttApplicationMessage> handler)
    {
        handlers.Add(handler);
        return this;
    }

    public async Task<MqttClientService> SubscribeAsync(params string[] topics)
    {
        if (topicFilters is not null)
        {
            await mqttClient.UnsubscribeAsync(new MqttClientUnsubscribeOptions
            {
                TopicFilters = topicFilters.Select(xx => xx.Topic).ToList()
            });
        }

        this.topicFilters = topics.Select(xx => new MqttTopicFilter
        {
            Topic = xx
            //, RetainHandling = MQTTnet.Protocol.MqttRetainHandling.SendAtSubscribe
        }).ToList();
        var subOpts = new MqttClientSubscribeOptions
        {
            TopicFilters = topicFilters
        };
        var result = await mqttClient.SubscribeAsync(subOpts);

        return this;
    }
}
