namespace NursingBot.Logger;

public class NLogLogger : ILogger
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public LogLevel LogLevel { get; set; }

    public async Task Info(string message) => await this.Log(LogLevel.INFO, message);

    public async Task Warn(string message) => await this.Log(LogLevel.WARN, message);

    public async Task Error(string message) => await this.Log(LogLevel.ERROR, message);

    public async Task Fatal(string message) => await this.Log(LogLevel.FATAL, message);

    public async Task Log(LogLevel logLevel, string message)
    {
        await Task.Run(() =>
        {
            if (logLevel < this.LogLevel)
            {
                return;
            }

            var nlogLevel = logLevel switch
            {
                LogLevel.INFO => NLog.LogLevel.Info,
                LogLevel.WARN => NLog.LogLevel.Warn,
                LogLevel.ERROR => NLog.LogLevel.Error,
                LogLevel.FATAL => NLog.LogLevel.Fatal,
                _ => throw new NotImplementedException("사용할 수 없는 로그 레벨")
            };

            Logger.Log(nlogLevel, message);
        });
    }
}