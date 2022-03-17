using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using NursingBot.Logger;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace NursingBot.Core
{
    public class Bot
    {
        public static readonly string DefaultCommandPrefix = "!";
        
        private static readonly string botStatus = "'!help'로 명령어 목록을 볼 수 있다고 안내";

        public CommandService CommandService { get; private set; }
        public DiscordSocketClient Client { get; private set; }

        private readonly IServiceProvider serviceProvider;

        public Bot()
        {
            this.Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged,
            });

            this.CommandService = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Info,

                CaseSensitiveCommands = false,
            });

            this.Client.Log += OnReceiveLog;
            this.CommandService.Log += OnReceiveLog;

            this.serviceProvider = ConfigureServices();
        }

        public async Task Initialize(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentNullException(nameof(token));
            }
            
            Client.Ready += this.OnClientReady;

            try
            {
                await this.Client.LoginAsync(TokenType.Bot, token);
                await this.Client.StartAsync();
            }
            catch (Exception e)
            {
                await Log.Fatal(e);
            }
        }

        private static async Task OnReceiveLog(LogMessage message)
        {
            if (message.Exception != null)
            {
                throw message.Exception;
            }

            await (message.Severity switch
            {
                LogSeverity.Info or LogSeverity.Verbose or LogSeverity.Debug
                    => Log.Info(message.Message),
                LogSeverity.Warning
                    => Log.Warn(message.Message),
                LogSeverity.Error
                    => Log.Error(message.Message),
                LogSeverity.Critical
                    => Log.Fatal(message.Message),
                _ => Task.CompletedTask
            });
        }

        private static IServiceProvider ConfigureServices()
        {
            var map = new ServiceCollection();

            return map.BuildServiceProvider();
        }

        private async Task OnClientReady()
        {
            try
            {
                await this.Client.SetGameAsync(botStatus);
                await this.CommandService.AddModulesAsync(Assembly.GetEntryAssembly(), this.serviceProvider);

                this.Client.MessageReceived += HandleCommandAsync;
                this.CommandService.CommandExecuted += OnCommandExecuted;
            }
            catch (AggregateException e)
            {
                await Task.WhenAll(
                    e.InnerExceptions.Select(inner => Log.Fatal(inner))
                );
            }
            catch (HttpException e)
            {
                await Log.Fatal(e);
            }
            catch (Exception e)
            {
                await Log.Fatal(e);
            }
        }

        private async Task OnCommandExecuted(Optional<CommandInfo> commandInfo, ICommandContext context, IResult result)
        {
            if (result.IsSuccess)
            {
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("실패")
                .WithDescription(result.ErrorReason)
                .Build();

            await context.Message.ReplyAsync(embed: embed);
        }

        private async Task HandleCommandAsync(SocketMessage data)
        {
            if (data is not SocketUserMessage msg)
            {
                return;
            }

            // 봇의 메세지에는 반응하지 않습니다.
            if (msg.Author.Id == this.Client.CurrentUser.Id || msg.Author.IsBot)
            {
                return;
            }

            // DM으로 받은 메세지에도 반응하지 않습니다.
            if (msg.Channel.GetChannelType() == ChannelType.DM)
            {
                return;
            }

            var commandPrefix = string.Empty;

            {
                using var conn = await Database.Open();
                using var cmd = conn.CreateCommand();

                if (msg.Channel is SocketGuildChannel channel)
                {
                    cmd.CommandText = $"SELECT prefix FROM servers WHERE discordUID={channel.Guild.Id}";
                    var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        commandPrefix = reader.GetString(0);
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(commandPrefix))
            {
                commandPrefix = DefaultCommandPrefix;
            }

            int pos = 0;

            if (msg.HasStringPrefix(commandPrefix, ref pos))
            {
                var context = new SocketCommandContext(this.Client, msg);
                await this.CommandService.ExecuteAsync(context, pos, this.serviceProvider);
            }
        }
    }
}