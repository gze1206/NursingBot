using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using NursingBot.Core;
using NursingBot.Feature.Preconditions;
using NursingBot.Logger;
using NursingBot.Models;

namespace NursingBot.Features
{
    public class CoreModule : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("help", "명령어 목록이 담긴 링크를 전송합니다.")]
        public async Task HelpAsync()
        {
            var builder = new EmbedBuilder()
                .WithTitle("명령어 목록입니다.");

            var helpUrl = DotNetEnv.Env.GetString("HELP_URL");
            if (!string.IsNullOrWhiteSpace(helpUrl))
            {
                builder.WithDescription($"[이 링크]({helpUrl})에서 확인 가능합니다!");
            }
            else
            {
                builder.WithDescription("저런...설정된 링크가 없네요!\n그냥 / 눌러서 보십쇼.");
            }


            await this.Context.Interaction.RespondAsync(embed: builder.Build(), ephemeral: true);
        }

        [SlashCommand("register", "이 서버에서 봇 명령어 사용이 가능하도록 등록합니다.")]
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
                await this.Context.Interaction.RespondAsync("서버 등록에 성공했습니다!", ephemeral: true);
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                await Log.Fatal(e);
                await this.Context.Interaction.RespondAsync($"서버 등록에 실패했습니다...\n{e.Message}", ephemeral: true);
            }
        }

        //[SlashCommand("test", "DBG")]
        //[RequireAdminPermission]
        //public async Task TestAsync()
        //{
        //    await Task.CompletedTask;
        //}
    }
}