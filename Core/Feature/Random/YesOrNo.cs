using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;
using NursingBot.Core;
using NursingBot.Logger;
using NRandom = NursingBot.Core.Random;

namespace NursingBot.Features.Random
{
    public class YesOrNoRegister : IFeatureMigration
    {
        public async Task Migrate(DiscordSocketClient client, ConcurrentBag<ApplicationCommandProperties> commandBag)
        {
            await Log.Info($"MIGRATE {YesOrNo.CommandName}");
            
            var builder = new SlashCommandBuilder()
                .WithName(YesOrNo.CommandName)
                .WithDescription(YesOrNo.CommandDescription)
                .AddOption("question", ApplicationCommandOptionType.String, "답을 듣고 싶은 질문을 입력하세요.", false);
            
            commandBag.Add(builder.Build());
        }
    }
    public class YesOrNo : IFeature
    {
        public static readonly string CommandName = "yn";
        public static readonly string CommandDescription = "당신의 질문에 Yes or No로 대답을 해드립니다.";

        public string Name => CommandName;

        public async Task ProcessCommand(SocketSlashCommand command)
        {
            var embed = new EmbedBuilder()
                .WithTitle("질문에 대한 답입니다.")
                .AddField("Question", command.Data.Options.FirstOrDefault()?.Value ?? "*(질문 없음)*")
                .AddField("Answer", NRandom.Next(2) == 0 ? "YES" : "NO")
                .Build();

            await command.RespondAsync(embed: embed);
        }
    }
}