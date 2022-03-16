using Discord;
using Discord.Commands;
using NursingBot.Features.Preconditions;
using NRandom = NursingBot.Core.Random;

namespace NursingBot.Features
{
    public class DiceModule : ModuleBase<SocketCommandContext>
    {
        private static readonly int MinFace = 2;
        private static readonly int MaxFace = 100;
        private static readonly string FormatErrorMessage = "주사위의 형식이 올바르지 않습니다.";
        private static readonly string FaceErrorMessage = $"주사위의 면이 올바르지 않습니다. 최소 {MinFace}부터 최대 {MaxFace}까지 지원합니다.";

        [Command("dice")]
        [Alias("d", "roll")]
        [Summary("다면체 주사위를 굴린 뒤 그 결과를 표시합니다.")]
        [RequireRegister]
        public Task RollAsync([Summary("주사위를 표현하는 식입니다.\n예시 - 1d6+1")] string eval)
        {
            var tokens = eval
                .Trim()
                .Replace("d", ";")      // amount와 나머지를 분리합니다.
                .Replace("-", "+-")     // bonus가 음수거나 양수거나 동시에 다룰 수 있게 형식을 변경합니다.
                .Replace("+", ";")     // faces와 bonus를 분리합니다.
                .Split(';');
            
            // amount, faces, bonus
            var values = new[] { 0, 0, 0 };

            if (tokens.Length < 1 || tokens.Length > values.Length)
            {
                return this.Context.Message.ReplyAsync(FormatErrorMessage);
            }

            for (int i = 0, max = tokens.Length; i < max; i++)
            {
                if (int.TryParse(tokens[i], out var num))
                {
                    values[i] = num;
                }
                else
                {
                    return this.Context.Message.ReplyAsync(FormatErrorMessage);
                }
            }

            if (values[1] < MinFace || values[1] > MaxFace)
            {
                return this.Context.Message.ReplyAsync(FaceErrorMessage);
            }

            List<int> results = new();
            for (var i = 0; i < values[0]; i++)
            {
                results.Add(NRandom.Next(values[1]) + 1);
            }

            if (values[2] != 0)
            {
                results.Add(values[2]);
            }

            var embed = new EmbedBuilder()
                .WithTitle("주사위를 굴렸습니다.")
                .AddField("Result", results.Sum())
                .AddField("Detail", string.Join('+', results).Replace("+-","-"))
                .Build();

            return this.Context.Message.ReplyAsync(embed: embed);
        }
    }
}