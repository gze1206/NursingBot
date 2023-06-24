using Discord;
using Discord.Net;
using Discord.WebSocket;
using NursingBot.Logger;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Discord.Interactions;

namespace NursingBot.Core;

public class Bot
{
    private static readonly string botStatus = "'/help'로 명령어 목록을 볼 수 있다고 안내";

    public InteractionService CommandService { get; private set; }
    public DiscordSocketClient Client { get; private set; }

    private readonly IServiceProvider serviceProvider;

    public Bot()
    {
        this.Client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.GuildMessages
                             | GatewayIntents.GuildMessageReactions
                             | GatewayIntents.Guilds
                             | GatewayIntents.GuildVoiceStates,
        });

        this.CommandService = new InteractionService(this.Client, new InteractionServiceConfig
        {
            LogLevel = LogSeverity.Info,
            DefaultRunMode = RunMode.Async,
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
                

        try
        {
            await this.CommandService.AddModulesAsync(Assembly.GetEntryAssembly(), this.serviceProvider);
            this.Client.InteractionCreated += this.OnInteractionCreated;
            this.Client.ButtonExecuted += this.OnButtonExecuted;
            this.Client.Ready += this.OnClientReady;
            this.CommandService.SlashCommandExecuted += this.OnSlashCommandExecuted;

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

        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        map.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
            loggingBuilder.AddNLog(config);
        });

        return map.BuildServiceProvider();
    }

    private async Task OnClientReady()
    {
        try
        {
            await RegisterCommands();
            await this.Client.SetGameAsync(botStatus);
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
        finally
        {
            this.Client.Ready -= OnClientReady;
        }
    }

    private async Task OnButtonExecuted(SocketMessageComponent arg)
    {
        var context = new SocketInteractionContext<SocketMessageComponent>(this.Client, arg);
        await this.CommandService.ExecuteCommandAsync(context, this.serviceProvider);
    }

    private async Task OnInteractionCreated(SocketInteraction arg)
    {
        try
        {
            var context = new SocketInteractionContext(this.Client, arg);
            await this.CommandService.ExecuteCommandAsync(context, this.serviceProvider);
        }
        catch
        {
            throw;
        }
    }

    private async Task OnSlashCommandExecuted(SlashCommandInfo commandInfo, IInteractionContext interactionContext, IResult result)
    {
        if (result.IsSuccess)
        {
            return;
        }
            
        var embed = new EmbedBuilder()
            .WithTitle($"{commandInfo.Name} 실패")
            .WithDescription(result.ErrorReason)
            .Build();

        await interactionContext.Interaction.RespondAsync(embed: embed, ephemeral: true);
    }

    private async Task RegisterCommands()
    {
        try
        {
            await this.CommandService.RegisterCommandsGloballyAsync(deleteMissing: true);
        }
        catch
        {
            throw;
        }
    }
}