using MQTTnet;

namespace UnifiedNamespaceLib.Services;

public interface IMessagingService: IService
{
    Task<MqttClientService> ConnectAsync(string clientId = null);
    MqttClientService Handle(Action<MqttApplicationMessage> handler);
    Task<MqttClientService> PublishAsync<TPayload>(string topic, TPayload payload, bool retainFlag = true);
    Task<MqttClientService> SubscribeAsync(params string[] topics);
}