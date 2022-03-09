using System.Collections.Concurrent;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using NursingBot.Features;
using NursingBot.Logger;

namespace NursingBot.Core
{
    public class Bot
    {
        private readonly DiscordSocketClient client;
        private readonly Dictionary<string, IFeature> features = new();
        private readonly List<IFeatureMigration> migrations = new();

        public Bot()
        {
            this.client = new DiscordSocketClient();
            this.client.Log += OnReceiveLog;
            this.client.SlashCommandExecuted += SlashCommandHandler;
        }

        public async Task Initialize(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentNullException(nameof(token));
            }
            
            client.Ready += this.OnClientReady;

            await this.client.LoginAsync(TokenType.Bot, token);
            await this.client.StartAsync();
        }

        public Bot AddFeature(IFeature feature)
        {
            this.features.Add(feature.Name, feature);
            return this;
        }

        public Bot AddMigration(IFeatureMigration migration)
        {
            this.migrations.Add(migration);
            return this;
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

        private async Task OnClientReady()
        {
            // 등록된 모든 Migration을 수행
            // SlashCommand를 등록하거나 제거하는 작업을 수행해야 함
            try
            {
                var commandBag = new ConcurrentBag<ApplicationCommandProperties>();

                await Task.WhenAll(
                    this.migrations.Select(migration => migration.Migrate(this.client, commandBag))
                );

                await this.client.BulkOverwriteGlobalApplicationCommandsAsync(commandBag.ToArray());
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

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (!this.features.TryGetValue(command.CommandName, out var feature))
            {
                throw NoRegisteredFeatureException.Instance;
            }

            try
            {
                await feature.ProcessCommand(command);
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
    }
}