using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace NursingBot.Logger;

public class NoRegisteredLoggerException : Exception
{
    public static NoRegisteredLoggerException Instance => new();
}

public static class Log
{
    public static LogLevel LogLevel
    {
        get => logLevel;
        set => UpdateLogLevel(value);
    }

    private static LogLevel logLevel = LogLevel.ALL;

    private static readonly ConcurrentBag<ILogger> loggers = new();

    public static async Task Add(ILogger logger)
    {
        await Task.Run(() => {
            logger.LogLevel = logLevel;
            loggers.Add(logger);
        });
    }

    public static async Task Info(string message)
    {
        if (loggers.IsEmpty)
        {
            throw NoRegisteredLoggerException.Instance;
        }

        await Task.WhenAll(loggers.Select(l => l.Info(message)));
    }

    public static async Task Warn(string message)
    {
        if (loggers.IsEmpty)
        {
            throw NoRegisteredLoggerException.Instance;
        }

        await Task.WhenAll(loggers.Select(l => l.Warn(message)));
    }

    public static async Task Error(string message)
    {
        if (loggers.IsEmpty)
        {
            throw NoRegisteredLoggerException.Instance;
        }

        await Task.WhenAll(loggers.Select(l => l.Error(message)));
    }

    public static async Task Fatal(string message)
    {
        if (loggers.IsEmpty)
        {
            throw NoRegisteredLoggerException.Instance;
        }

        await Task.WhenAll(loggers.Select(l => l.Fatal(message)));
    }

    public static Task Fatal(Exception e)
        => Fatal($"{e.Message}\n{e.StackTrace}");

    public static Task Fatal(Discord.Net.HttpException e)
        => Fatal($"{e.Message}\n{JsonConvert.SerializeObject(e.Errors, Formatting.Indented)}");

    private static void UpdateLogLevel(LogLevel value)
    {
        if (loggers.IsEmpty)
        {
            throw NoRegisteredLoggerException.Instance;
        }

        logLevel = value;

        foreach (var logger in loggers)
        {
            logger.LogLevel = value;
        }
    }
}