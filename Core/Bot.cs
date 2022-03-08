using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
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

            client.Ready += this.OnClientReady;
        }

        private async Task OnClientReady()
        {
            {
                try
                {
                    var command = new SlashCommandBuilder()
                        .WithName("gtest")
                        .WithDescription("gtest");

                    var guild = this.client.GetGuild(320908910426980352);
                    await guild.CreateApplicationCommandAsync(command.Build());
                }
                catch (HttpException ex)
                {
                    var json = JsonConvert.SerializeObject(ex.Errors);
                    await Log.Fatal($"{ex.Message}\n\t{json}");
                }
            }

            {
                try
                {
                    var command = new SlashCommandBuilder()
                        .WithName("test")
                        .WithDescription("test test test test test");

                    await this.client.CreateGlobalApplicationCommandAsync(command.Build());
                }
                catch (HttpException ex)
                {
                    var json = JsonConvert.SerializeObject(ex.Errors);
                    await Log.Fatal($"{ex.Message}\n\t{json}");
                }
                catch (Exception ex)
                {
                    await Log.Fatal($"{ex.Message}");
                }
            }
        }
    }
}