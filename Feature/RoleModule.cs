using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using NursingBot.Core;
using NursingBot.Feature.Preconditions;
using NursingBot.Logger;
using NursingBot.Models;

namespace NursingBot.Feature;

[RequireRegister]
[Group("role", "역할을 쉽게 할당하고 해제할 수 있도록 하는 기능")]
public class RoleModule : InteractionModuleBase<SocketInteractionContext>
{
    [Group("manager", "역할 관리 메시지를 관리합니다.")]
    public class RoleManagerModel : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("add", "새 역할 관리 메시지를 만듭니다.")]
        [RequireAdminPermission]
        public async Task AddManagerAsync([Summary("channel", "메시지를 등록할 채널")] ITextChannel channel,
            [Summary("title", "제목")] string title, [Summary("description", "설명")] string? description = null)
        {
            if (!Database.CachedServers.TryGetValue(channel.GuildId, out var server))
            {
                await this.Context.Interaction.RespondAsync("서버 정보 조회에 실패했습니다.");
                return;
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                description = null;
            }

            await using var context = await Database.Instance.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                await this.Context.Interaction.DeferAsync();

                var embed = new EmbedBuilder()
                    .WithTitle(title);

                if (description != null)
                {
                    embed.WithDescription(description);
                }
                
                var msg = await channel.SendMessageAsync(embed: embed.Build());
                if (msg == null)
                {
                    throw new ApplicationException("메시지 전송에 실패했습니다.");
                }

                var manager = new RoleManager()
                {
                    ServerId = server.Id,
                    Title = title,
                    Description = description,
                    ChannelId = channel.Id,
                    MessageId = msg.Id
                };

                await context.RoleManagers.AddAsync(manager);
                await context.SaveChangesAsync();

                await transaction.CommitAsync();
                await this.Context.Interaction.FollowupAsync("역할 관리 메시지 생성에 성공했습니다.");
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                await Log.Fatal(e);
                await this.Context.Interaction.FollowupAsync("역할 관리 메시지 생성에 실패했습니다.", ephemeral: true);
            }
        }

        [SlashCommand("remove", "역할 관리 메시지를 제거합니다.")]
        [RequireAdminPermission]
        public async Task RemoveManagerAsync([Summary("manager"), Autocomplete(typeof(RoleManagerAutocompleteHandler))] ulong managerId)
        {
            if (!Database.CachedServers.TryGetValue(this.Context.Guild.Id, out var server))
            {
                await this.Context.Interaction.RespondAsync("서버 정보 조회에 실패했습니다.");
                return;
            }

            if (managerId == 0)
            {
                await this.Context.Interaction.RespondAsync("알 수 없는 역할 관리 메시지입니다.", ephemeral: true);
                return;
            }

            await using var context = await Database.Instance.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();
            var now = DateTime.UtcNow;

            try
            {
                await this.Context.Interaction.DeferAsync();

                var manager = await context.RoleManagers.FirstOrDefaultAsync(m => m.Id == managerId);
                if (manager == null)
                {
                    throw new Exception("역할 관리 메시지를 찾지 못했습니다.");
                }

                var channel = this.Context.Guild.GetTextChannel(manager.ChannelId);
                if (channel == null)
                {
                    throw new Exception($"{manager.ChannelId} 채널을 찾지 못했습니다.");
                }

                var msg = await channel.GetMessageAsync(manager.MessageId);
                if (msg == null)
                {
                    throw new Exception($"{manager.MessageId} 메시지를 찾지 못했습니다.");
                }

                await msg.DeleteAsync();

                if (manager.Roles?.Length > 0)
                {
                    context.Roles.RemoveRange(manager.Roles);
                }
                context.RoleManagers.Remove(manager);
                
                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                await Log.Info($"RoleManager 삭제 : {manager.Title} by {this.Context.User.Username}");
                await this.Context.Interaction.FollowupAsync("역할 관리 메시지 삭제에 성공했습니다.");
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                await Log.Fatal(e);
                await this.Context.Interaction.FollowupAsync($"역할 관리 메시지 삭제에 실패했습니다.\n{e.Message}", ephemeral: true);
            }
        }
    }

    [SlashCommand("add", "부여 가능한 역할을 등록합니다.")]
    [RequireAdminPermission]
    public async Task AddRoleAsync(
        [Summary("manager", "역할 관리 메시지"), Autocomplete(typeof(RoleManagerAutocompleteHandler))] ulong managerId,
        [Summary("role", "역할")] IRole discordRole,
        [Summary("emoji", "이모지")] IEmote emoji)
    {
        
    }

    private class RoleManagerAutocompleteHandler : AutocompleteHandler
    {
        private const int ApiLimit = 25;
        
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter, IServiceProvider services)
        {
            if (!Database.CachedServers.TryGetValue(context.Guild.Id, out var server))
            {
                return AutocompletionResult.FromError(new Exception("등록되지 않은 서버입니다."));
            }

            await using var dbContext = await Database.Instance.CreateDbContextAsync();
            var managers = dbContext.RoleManagers
                .Where(r => r.ServerId == server.Id)
                .Select(r => new AutocompleteResult(r.Title, r.Id));
            
            return AutocompletionResult.FromSuccess(managers.Take(ApiLimit));
        }
    }
}