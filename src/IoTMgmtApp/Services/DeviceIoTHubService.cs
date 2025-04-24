using IoTMgmtApp.Models;
using MQTTnet;
using MQTTnet.Client;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace IoTMgmtApp.Services;

public class DeviceIoTHubService: BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var EpochTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        var hostName = "TheThingsHub-42-01-neu.azure-devices.net";
        var portNumber = 8883;
        var deviceId = "NeoForgeIndustries-Device17";

        const int QoS_AT_MOST_ONCE = 1;

        var clientId = deviceId;
        var resourceId = $"{hostName}/devices/{deviceId}";
        var username = $"{hostName}/{deviceId}/api-version=2021-04-12";
        // https://learn.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-messages-d2c#azure-storage-as-a-routing-endpoint
        // https://learn.microsoft.com/en-us/azure/iot/iot-mqtt-connect-to-iot-hub#receiving-cloud-to-device-messages
        // https://learn.microsoft.com/en-us/azure/iot/iot-mqtt-connect-to-iot-hub#sending-device-to-cloud-messages
        var devicePublishTopic = $"devices/{deviceId}/messages/events/$.ct=application%2Fjson%3Bcharset%3Dutf-8";
        var deviceSubscribeTopic = $"devices/{deviceId}/messages/devicebound";

        // https://learn.microsoft.com/en-us/cli/azure/iot/hub?view=azure-cli-latest#az-iot-hub-generate-sas-token-examples
        var primaryKey = "Id2WWXxdQUyqdBukpnyCCkEN2Bz6Nm1z6CJNve8AGoI=";
        // az iot hub generate-sas-token -d NeoForgeIndustries-Device17 -n TheThingsHub-42-01-neu --duration 1000000
        //{
        //    "sas": "SharedAccessSignature sr=TheThingsHub-neu-01.azure-devices.net%2Fdevices%2Fneoforgeindustries-device01&sig=rwlotjkxDgeXh7fF7EQYnoT6xVCl9NvfDmn2%2FE2MRR8%3D&se=1740608899"
        //}
        var password = CreateToken(resourceId, primaryKey);
        //var password = "SharedAccessSignature sr=TheThingsHub-neu-01.azure-devices.net%2Fdevices%2Fneoforgeindustries-device01&sig=rwlotjkxDgeXh7fF7EQYnoT6xVCl9NvfDmn2%2FE2MRR8%3D&se=1740608899";

        // https://sandervandevelde.wordpress.com/2022/08/12/exploring-full-azure-iot-hub-device-support-using-mqtts-only/

        var mqttClient = new MqttFactory().CreateMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithClientId(clientId)
            .WithTcpServer(hostName, portNumber) // Port is optional
            .WithCredentials(username, password)
            .WithTlsOptions(new MqttClientTlsOptions
            {
                AllowUntrustedCertificates = true,
                UseTls = true,
            })
            .WithCleanSession()
            .Build();

        await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        //mqttClient.UseApplicationMessageReceivedHandler(args => {

        //    Console.WriteLine($"Message received: {args.ApplicationMessage.Topic}");
        //    Console.WriteLine($"Payload: {Encoding.UTF8.GetString(args.ApplicationMessage.Payload)}");

        //});
        //mqttClient.UseConnectedHandler(async e =>
        //{
        //    Console.WriteLine("### CONNECTED WITH SERVER ###");

        //    // Subscribe to a topic
        //    var result = await mqttClient.SubscribeAsync(topicFilter);

        //    Console.WriteLine("### SUBSCRIBED ###");
        //});


        while (true)
        {
            var now = DateTimeOffset.Now;

            var message = new AIOMessagePayload
            {
                Timestamp = now,
                MessageType = "ua-deltaframe",
                Payload = new Dictionary<string, AIOMessagePayloadItem>
                {
                    ["State"] = new AIOMessagePayloadItem { SourceTimestamp = now, Value = "running" },
                    ["AlarmSubsystem1"] = new AIOMessagePayloadItem { SourceTimestamp = now, Value = true },
                    ["AlarmSubsystem2"] = new AIOMessagePayloadItem { SourceTimestamp = now, Value = false },
                    ["AbsorbedEnergy"] = new AIOMessagePayloadItem { SourceTimestamp = now, Value = Random.Shared.NextDouble() * 100 }
                },
                DataSetWriterName = $"{deviceId}",
                SequenceNumber = Random.Shared.Next(0, 100000)
            };

            var messageJson = JsonSerializer.Serialize(message);

            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(devicePublishTopic)
                .WithPayload(JsonSerializer.Serialize(message))
                .Build();

            await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

            Console.WriteLine("Message was published.");

            await Task.Delay(1000);
        }

        await mqttClient.DisconnectAsync();

        string CreateToken(string resourceUri, string symmetricKey, int timeToLive = 86400)
        {
            var sinceEpoch = DateTime.UtcNow - EpochTime;
            var expiry = Convert.ToString((int)sinceEpoch.TotalSeconds + timeToLive);
            string resourceUriEncoded = WebUtility.UrlEncode(resourceUri);

            string value;
            using (HMACSHA256 hMACSHA = new HMACSHA256(Convert.FromBase64String(symmetricKey)))
            {
                value = Convert.ToBase64String(hMACSHA.ComputeHash(Encoding.UTF8.GetBytes($"{resourceUriEncoded}\n{expiry}")));
            }

            return $"SharedAccessSignature sr={resourceUriEncoded}&sig={WebUtility.UrlEncode(value)}&se={expiry}";
        }
    }
}
