namespace NursingBot.Logger;

public interface ILogger
{
    LogLevel LogLevel { get; set; }

    Task Info(string message);
    Task Warn(string message);
    Task Error(string message);
    Task Fatal(string message);
    Task Log(LogLevel logLevel, string message);
}