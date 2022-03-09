using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;
using NursingBot.Core;
using NursingBot.Logger;
using NRandom = NursingBot.Core.Random;

namespace NursingBot.Features.Random
{
    public class Magic8BallRegister : IFeatureMigration
    {
        public async Task Migrate(DiscordSocketClient client, ConcurrentBag<ApplicationCommandProperties> commandBag)
        {
            await Log.Info($"MIGRATE {Magic8Ball.CommandName}");

            var builder = new SlashCommandBuilder()
                .WithName(Magic8Ball.CommandName)
                .WithDescription(Magic8Ball.CommandDescription)
                .AddOption("question", ApplicationCommandOptionType.String, "답을 듣고 싶은 질문을 입력하세요.", true);
            
            commandBag.Add(builder.Build());
        }
    }
    public class Magic8Ball : IFeature
    {
        public static readonly string CommandName = "m8b";
        public static readonly string CommandDescription = "당신의 질문에 임의로 대답을 해드립니다.";

        public string Name => CommandName;

        private static readonly string[] answers = new[]
        {
            "확실합니다.", "확실하게 그렇습니다.", "의심의 여지가 없네요.", "확실히 예라고 답하겠습니다.", "믿으셔도 됩니다.",
            "제가 보기엔 그렇습니다.", "가장 가능성이 높네요.", "전망이 좋습니다.", "예.", "애매하네요.", "나중에 다시 물어보세요.",
            "지금은 예측할 수 없네요.", "질문을 조금 더 다듬어주세요.", "기대하지 마세요.", "아니오.", "제가 듣기론 아니네요.", "전망이 그리 좋지 않아요.",
            "심히 의심됩니다."
        };

        public async Task ProcessCommand(SocketSlashCommand command)
        {
            var embed = new EmbedBuilder()
                .WithTitle("질문에 대한 답입니다.")
                .AddField("Question", command.Data.Options.FirstOrDefault()?.Value ?? "*(질문 없음. 어케함?)*")
                .AddField("Answer", answers[NRandom.Next(answers.Length)])
                .Build();

            await command.RespondAsync(embed: embed);
        }
    }
}