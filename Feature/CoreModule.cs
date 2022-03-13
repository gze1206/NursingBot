using System.Text;
using Discord;
using Discord.Commands;

namespace NursingBot.Features
{
    public class CoreModule : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        [Alias("?", "h")]
        [Summary("명령어 목록을 DM으로 전송합니다.")]
        public Task HelpAsync()
        {
            var builder = new EmbedBuilder()
                .WithTitle("명령어 목록입니다.");

            var commands = Program.Bot?.CommandService.Commands.ToList() ?? new();
            foreach (var cmd in commands)
            {
                var stringBuilder = new StringBuilder(cmd.Summary ?? "*설명 없음*");

                if (cmd.Aliases.Count > 1)
                {
                    stringBuilder.Append($"\n\n**별칭** : {string.Join(',', cmd.Aliases.Skip(1))}");
                }

                if (cmd.Parameters.Count > 0)
                {
                    stringBuilder.Append("\n\n**매개 변수**");
                    foreach (var param in cmd.Parameters)
                    {
                        stringBuilder.Append($"\n* {param.Name} : {(param.IsOptional?"*(선택)* ":"")}{param.Summary}");
                    }
                }

                builder.AddField(cmd.Name, stringBuilder);
            }

            return this.Context.User.SendMessageAsync(embed: builder.Build());
        }
    }
}