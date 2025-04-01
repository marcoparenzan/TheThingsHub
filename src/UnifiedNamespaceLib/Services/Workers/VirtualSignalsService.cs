using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using PowerFxLib.Models;
using System.Buffers;
using System.Text;
using UnifiedNamespaceLib.Services;
using UnifiedNamespaceLib.Services;
using YamlDotNet.RepresentationModel;

namespace UnifiedNamespaceLib.Services.Workers;

public class VirtualSignalsService(IConfiguration config, IServiceProvider sp, ILogger<VirtualSignalsService> logger) : IWorkerService
{
    private PowerFxValue virtualSignalDefs;
    private RecalcEngine powerFxEngine;

    public async Task ExecuteAsync()
    {
        var serviceKey = nameof(VirtualSignalsService);
        var filename = config[$"{serviceKey}:fileName"];
        var yamlStream = new YamlStream();
        var yeamlReader = new StreamReader(filename);
        yamlStream.Load(yeamlReader);
        virtualSignalDefs = PowerFxValue.From(yamlStream.First().RootNode);
        logger.LogInformation($"{serviceKey}: loaded {filename} configuration file");

        // POWERFX
        var powerFxConfig = new PowerFxConfig(Features.PowerFxV1);
        powerFxEngine = new RecalcEngine(powerFxConfig);

        // mqttHandling
        var messaging = sp.GetService<IMessagingService>();

        messaging.Handle(async msg =>
        {
            if (msg.Topic.StartsWith("timers/"))
            {
                foreach (var (key, value) in virtualSignalDefs.SetValue)
                {
                    if (value.ValueType == PowerFxValueType.Formula)
                    {
                        var virtualSignalStatusTopic = $"virtualSignals/{key}/status";
                        try
                        {
                            var result = await powerFxEngine.EvalAsync(
                                value.FormulaValue,
                                CancellationToken.None
                            );
                            if (result.Type == FormulaType.Decimal)
                            {
                                await messaging.PublishAsync(key, result.AsDecimal());
                                await messaging.PublishAsync(virtualSignalStatusTopic, "ok");
                            }
                            else if (result.Type == FormulaType.String)
                            {
                                await messaging.PublishAsync(key, result.ToObject());
                                await messaging.PublishAsync(virtualSignalStatusTopic, "ok");
                            }
                            else
                            {
                                await messaging.PublishAsync(virtualSignalStatusTopic, "data type unknown");
                            }
                        }
                        catch (Exception ex)
                        {
                            await messaging.PublishAsync(virtualSignalStatusTopic, ex.Message);
                        }
                    }
                    else
                    {
                        await messaging.PublishAsync(key, value);
                    }
                }
            }
            else // cache filled
            {
                var json = Encoding.UTF8.GetString(msg.Payload.ToArray());
                // collect
                var value = FormulaValueJSON.FromJson(json);
                powerFxEngine.UpdateVariable(msg.Topic, value);
            }
        });

        await messaging.ConnectAsync(serviceKey);
        await messaging.SubscribeAsync("#");
    }
}