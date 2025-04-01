using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet.Protocol;
using MQTTnet.Server;
using UnifiedNamespaceLib.Models;

namespace UnifiedNamespaceLib.Services.Workers;

public class MqttBrokerService(IConfiguration config, IServiceProvider sp, ILogger<MqttBrokerService> logger) : IWorkerService
{
    private MqttServer server;

    public async Task ExecuteAsync()
    {
        var retain = sp.GetService<IRetainedMessageService>();

        var mqttServerFactory = new MqttServerFactory();

        // Due to security reasons the "default" endpoint (which is unencrypted) is not enabled by default!
        var mqttServerOptions = mqttServerFactory
            .CreateServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(2883)
            .Build();

        server = mqttServerFactory.CreateMqttServer(mqttServerOptions);
        server.ClientConnectedAsync += async (o) =>
        {
            // retained messages
            Console.WriteLine($"{o.ClientId} connected");
        };

        server.ValidatingConnectionAsync += async e =>
        {
            //if (e.ClientId != "dt")
            //{
            //    e.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
            //    return;
            //}

            if (e.UserName != "ValidUser")
            {
                e.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                return;
            }

            if (e.Password != "SecretPassword")
            {
                e.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                return;
            }

            e.ReasonCode = MqttConnectReasonCode.Success;
        };

        // Make sure that the server will load the retained messages.
        server.LoadingRetainedMessageAsync += async eventArgs =>
        {
            var messages = await retain.GetAsync();
            eventArgs.LoadedRetainedMessages = messages.Select(xx => xx.ToApplicationMessage()).ToList();
        };

        // Make sure to persist the changed retained messages.
        server.RetainedMessageChangedAsync += async eventArgs =>
        {
            await retain.AddAsync(MqttRetainedMessageModel.Create(eventArgs.ChangedRetainedMessage));
        };

        server.RetainedMessagesClearedAsync += async (a) =>
        {
        };

        await server.StartAsync();
    }
}
