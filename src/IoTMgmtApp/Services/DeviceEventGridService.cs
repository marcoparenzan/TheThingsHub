using IoTMgmtApp.Models;
using MQTTnet;
using MQTTnet.Client;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace IoTMgmtApp.Services;

public class DeviceEventGridService: BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var hashids = new HashidsNet.Hashids();

        var hostname = "neoforgeindustries-thethingshub-uns.northeurope-1.ts.eventgrid.azure.net";
        //var hostname = "mpeg.swedencentral-1.ts.eventgrid.azure.net";

        var customerName = "neoforgeindustries";
        var deviceName = "device02";
        var cn = $"{customerName}-{deviceName}";
        var password = hashids.Encode(cn.Select(xx => (int)xx));
        var clientId = cn;
        var username = cn;

        var x509_pem = @$"certs\{cn}.pem";  //Provide your client certificate .cer.pem file path
        var x509_key = @$"certs\{cn}.key";  //Provide your client certificate .key.pem file path

        var xxx = X509Certificate2.CreateFromPemFile(x509_pem, x509_key);
        var yyy = xxx.Export(X509ContentType.Pkcs12);
        var certificate = new X509Certificate2(yyy);

        clientId = "device06";
        username = clientId;
        var bytes = File.ReadAllBytes($@"D:\IoTHub\TheIoTThing\src\ConsoleApp2\bin\Debug\net9.0\certs\{clientId}-cert.pfx");
        certificate = new X509Certificate2(bytes, "password");

        var mqttClient = new MqttFactory().CreateMqttClient();

        var opts = new MqttClientOptionsBuilder()
            .WithTcpServer(hostname, 8883)
            .WithClientId(clientId)
            .WithCredentials(username, "")  //use client authentication name in the username
            .WithTlsOptions(configure =>
                configure
                    .UseTls()
                    .WithClientCertificates([certificate])
            )
            .Build();

        try
        {
            var connAck = await mqttClient!.ConnectAsync(opts);
            Console.WriteLine($"Client Connected: {mqttClient.IsConnected} with CONNACK: {connAck.ResultCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to MQTT broker: {ex.Message}");
            return;
        }

        mqttClient.ApplicationMessageReceivedAsync += async m => await Console.Out.WriteAsync($"Received message on topic: '{m.ApplicationMessage.Topic}' with content: '{m.ApplicationMessage.ConvertPayloadToString()}'\n\n");

        //var suback = await mqttClient.SubscribeAsync("test1/topic1");
        //suback.Items.ToList().ForEach(s => Console.WriteLine($"subscribed to '{s.TopicFilter.Topic}' with '{s.ResultCode}'"));

        while (true)
        {
            var now = DateTimeOffset.Now;

            var message = new AIOMessagePayload
            {
                Timestamp = now,
                MessageType = "ua-deltaframe",
                Payload = new Dictionary<string, AIOMessagePayloadItem>
                {
                    ["State"] = new() { SourceTimestamp = now, Value = "running" },
                    ["AlarmSubsystem1"] = new() { SourceTimestamp = now, Value = true },
                    ["AlarmSubsystem2"] = new() { SourceTimestamp = now, Value = false },
                    ["AbsorbedEnergy"] = new() { SourceTimestamp = now, Value = Random.Shared.NextDouble() * 100 }
                },
                DataSetWriterName = cn,
                SequenceNumber = Random.Shared.Next(0, 100000)
            };

            var messageJson = JsonSerializer.Serialize(message);

            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic("devicetelemetry/topic1")
                //.WithTopic("azure-iot-operations/data/opc.tcp/opc.tcp-1/solarcamp-north")
                .WithPayload(JsonSerializer.Serialize(message))
                .Build();

            var puback = await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);
            Console.WriteLine(puback.ReasonString);
            await Task.Delay(5000);
        }

        await mqttClient.DisconnectAsync();
    }
}
