using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet;
using System.Buffers;

namespace UnifiedNamespaceLib.Models;

public class MqttRetainedMessageModel
{
    public string? ContentType { get; set; }
    public byte[]? CorrelationData { get; set; }
    public byte[]? Payload { get; set; }
    public MqttPayloadFormatIndicator PayloadFormatIndicator { get; set; }
    public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }
    public string? ResponseTopic { get; set; }
    public string? Topic { get; set; }
    public MqttUserProperty[] UserProperties { get; set; }

    public static MqttRetainedMessageModel Create(MqttApplicationMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new MqttRetainedMessageModel
        {
            Topic = message.Topic,

            // Create a copy of the buffer from the payload segment because
            // it cannot be serialized and deserialized with the JSON serializer.
            Payload = message.Payload.ToArray(),
            UserProperties = (message.UserProperties ?? []).ToArray(),
            ResponseTopic = message.ResponseTopic,
            CorrelationData = message.CorrelationData,
            ContentType = message.ContentType,
            PayloadFormatIndicator = message.PayloadFormatIndicator,
            QualityOfServiceLevel = message.QualityOfServiceLevel

            // Other properties like "Retain" are not if interest in the storage.
            // That's why a custom model makes sense.
        };
    }

    public MqttApplicationMessage ToApplicationMessage()
    {
        return new MqttApplicationMessage
        {
            Topic = Topic,
            PayloadSegment = new ArraySegment<byte>(Payload ?? Array.Empty<byte>()),
            PayloadFormatIndicator = PayloadFormatIndicator,
            ResponseTopic = ResponseTopic,
            CorrelationData = CorrelationData,
            ContentType = ContentType,
            UserProperties = UserProperties.ToList(),
            QualityOfServiceLevel = QualityOfServiceLevel,
            Dup = false,
            Retain = true
        };
    }
}
