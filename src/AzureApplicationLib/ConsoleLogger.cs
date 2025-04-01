using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureApplicationLib;

public class ConsoleLogger : ILogger<ConsoleLogger>
{
    public static ILogger Default = new ConsoleLogger();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return default;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Console.WriteLine($"NOT IMPLEMENTED {logLevel} {eventId} {state} {exception}");
    }
}
