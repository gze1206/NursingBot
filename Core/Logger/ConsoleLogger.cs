namespace NursingBot.Logger
{
    public class ConsoleLogger : ILogger
    {
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

                Console.WriteLine($"{logLevel} : {message}");
            });
        }
    }
}