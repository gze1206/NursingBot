using Discord;
using Discord.WebSocket;
using NursingBot.Logger;

namespace NursingBot.Core
{
    public class Bot
    {
        private readonly DiscordSocketClient client;

        public Bot()
        {
            this.client = new DiscordSocketClient();
            this.client.Log += (msg) => {
                if (msg.Exception != null)
                {
                    throw msg.Exception;
                }

                return msg.Severity switch
                {
                    LogSeverity.Info or LogSeverity.Verbose or LogSeverity.Debug => Log.Info(msg.Message),
                    LogSeverity.Warning => Log.Warn(msg.Message),
                    LogSeverity.Error => Log.Error(msg.Message),
                    LogSeverity.Critical => Log.Fatal(msg.Message),
                    _ => Task.CompletedTask
                };
            };
        }

        public async Task Initialize(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentNullException(nameof(token));
            }
            
            await this.client.LoginAsync(TokenType.Bot, token);
            await this.client.StartAsync();
        }
    }
}