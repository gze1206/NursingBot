using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;
using NursingBot.Core;
using NursingBot.Logger;
using NRandom = NursingBot.Core.Random;

namespace NursingBot.Features.Random
{
    public class PickRegister : IFeatureMigration
    {
        public async Task Migrate(DiscordSocketClient client, ConcurrentBag<ApplicationCommandProperties> commandBag)
        {
            await Log.Info($"MIGRATE {Pick.CommandName}");
            
            var builder = new SlashCommandBuilder()
                .WithName(Pick.CommandName)
                .WithDescription(Pick.CommandDescription)
                .AddOption(Pick.QuestionOptionName, ApplicationCommandOptionType.String, "답을 듣고 싶은 질문을 입력하세요.", true)
                .AddOption(Pick.AnswersOptionName, ApplicationCommandOptionType.String, "답변 후보를 입력해주세요. 항목은 ;로 구분합니다. 이 값이 생략되거나 항목이 하나 뿐일 경우 Yes or No로 대답합니다.", false);
            
            commandBag.Add(builder.Build());
        }
    }
    public class Pick : IFeature
    {
        public static readonly string CommandName = "pick";
        public static readonly string CommandDescription = "당신의 질문에 대답을 해드립니다. 답변은 입력한 목록 중 무작위로 선택됩니다.";

        public static readonly string QuestionOptionName = "question";
        public static readonly string AnswersOptionName = "answers";

        private const char AnswerDelimiter = ';';

        public string Name => CommandName;

        public async Task ProcessCommand(SocketSlashCommand command)
        {
            var answersRaw = command.Data.Options.FirstOrDefault(opt => opt.Name.Equals(AnswersOptionName))?.Value as string ?? string.Empty;
            var answers = answersRaw.Split(AnswerDelimiter);
            if (answers.Length < 2)
            {
                answers = new[]
                {
                    "YES", "NO"
                };
            }

            var embed = new EmbedBuilder()
                .WithTitle("질문에 대한 답입니다.")
                .AddField("Question", command.Data.Options.First(opt => opt.Name.Equals(QuestionOptionName)).Value)
                .AddField("Answer", answers[NRandom.Next(answers.Length)])
                .Build();

            await command.RespondAsync(embed: embed);
        }
    }
}