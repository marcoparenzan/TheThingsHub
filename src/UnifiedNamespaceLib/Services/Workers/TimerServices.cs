using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UnifiedNamespaceLib.Services;
using UnifiedNamespaceLib.Services;

namespace UnifiedNamespaceLib.Services.Workers;

public class TimerServices(IConfiguration config, IServiceProvider sp, ILogger<TimerServices> logger) : IWorkerService
{
    public async Task ExecuteAsync()
    {
        var messaging = sp.GetKeyedService<IMessagingService>("MqttClientTimers");

        await messaging.ConnectAsync();

        var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await periodicTimer.WaitForNextTickAsync())
        {
            var now = DateTimeOffset.Now;
            var body = new
            {
                now
            };

            if (now.Second % 5 == 0)
            {
                await messaging.PublishAsync($"timers/5s", body, retainFlag: false);
                if (now.Second % 15 == 0)
                {
                    await messaging.PublishAsync($"timers/15s", body, retainFlag: false);
                }
                if (now.Second == 0)
                {
                    await messaging.PublishAsync($"timers/1m", body, retainFlag: false);
                    if (now.Minute == 0)
                    {
                        await messaging.PublishAsync($"timers/1h", body, retainFlag: false);
                    }
                }
            }
        }
    }
}
