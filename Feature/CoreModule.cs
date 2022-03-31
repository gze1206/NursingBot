using System.Text;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using NursingBot.Core;
using NursingBot.Feature.Preconditions;
using NursingBot.Logger;
using NursingBot.Models;

namespace NursingBot.Features
{
    public class CoreModule : ModuleBase<SocketCommandContext>
    {
        private static readonly string DEBUG_SUMMARY = "DBG";

        [Command("help")]
        [Alias("?", "h")]
        [Summary("명령어 목록을 DM으로 전송합니다.")]
        public async Task HelpAsync()
        {
            var builder = new EmbedBuilder()
                .WithTitle("명령어 목록입니다.");

            var helpUrl = DotNetEnv.Env.GetString("HELP_URL");
            if (!string.IsNullOrWhiteSpace(helpUrl))
            {
                builder.WithDescription($"[이 링크]({helpUrl})에서도 확인 가능합니다!");
            }

            var modules = Global.Bot?.CommandService.Modules.ToList() ?? new();
            foreach (var module in modules)
            {
                var group = string.Empty;
                if (module.Group != null)
                {
                    group = $"{module.Group} ";
                }
                var commands = module.Commands.ToList() ?? new();
                foreach (var cmd in commands)
                {
                    var precondition = await cmd.CheckPreconditionsAsync(this.Context);
                    if (!precondition.IsSuccess)
                    {
                        continue;
                    }

                    if (cmd.Summary?.StartsWith(DEBUG_SUMMARY) ?? false)
                    {
                        continue;
                    }
                    
                    var stringBuilder = new StringBuilder(cmd.Summary ?? "*설명 없음*");

                    if (cmd.Aliases.Count > 1)
                    {
                        stringBuilder.Append($"\n\n**별칭** : {string.Join(',', cmd.Aliases.Skip(1))}");
                    }

                    if (cmd.Parameters.Count > 0)
                    {
                        stringBuilder.Append("\n\n* **매개 변수**");
                        foreach (var param in cmd.Parameters)
                        {
                            stringBuilder.Append($"\n　　* {param.Name} : {(param.IsOptional?"*(선택)* ":"")}{param.Summary}");
                        }
                    }

                    builder.AddField($"{Bot.DefaultCommandPrefix}{group}{cmd.Name}", stringBuilder);
                }
            }

            await this.Context.User.SendMessageAsync(embed: builder.Build());
        }

        [Command("register")]
        [Summary("이 서버에서 봇 명령어 사용이 가능하도록 등록합니다.")]
        [RequireAdminPermission]
        public async Task RegisterAsync()
        {
            var server = new Server
            {
                DiscordUID = this.Context.Guild.Id
            };


            using var context = await Database.Instance.CreateDbContextAsync();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {

                if (await context.Servers.AnyAsync(s => s.DiscordUID == server.DiscordUID))
                {
                    throw new Exception("이미 등록된 서버입니다.");
                }

                await context.Servers.AddAsync(server);
                await context.SaveChangesAsync();

                await transaction.CommitAsync();

                Database.Cache(server.DiscordUID, server);
                await this.ReplyAsync("서버 등록에 성공했습니다!");
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                await Log.Fatal(e);
                await this.ReplyAsync($"서버 등록에 실패했습니다...\n{e.Message}");
            }
        }

        [Command("test")]
        [Summary("DBG")]
        [RequireAdminPermission]
        public async Task TestAsync()
        {
            await Task.CompletedTask;
        }
    }
}