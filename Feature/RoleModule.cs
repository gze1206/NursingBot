using System.Text;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
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

            try
            {
                await this.Context.Interaction.DeferAsync();

                var manager = await context.RoleManagers.FirstOrDefaultAsync(m => m.ServerId == server.Id && m.Id == managerId);
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

    public override void OnModuleBuilding(InteractionService commandService, ModuleInfo module)
    {
        base.OnModuleBuilding(commandService, module);

        if (Global.Bot == null)
        {
            return;
        }

        Global.Bot.OnReactionAdded += (_, _, reaction) => OnReactionAdded(reaction);
        Global.Bot.OnReactionRemoved += (_, _, reaction) => OnReactionRemoved(reaction);
    }

    [SlashCommand("add", "부여 가능한 역할을 등록합니다.")]
    [RequireAdminPermission]
    public async Task AddRoleAsync(
        [Summary("manager", "역할 관리 메시지"), Autocomplete(typeof(RoleManagerAutocompleteHandler))] ulong managerId,
        [Summary("role", "역할")] IRole discordRole,
        [Summary("emoji", "이모지")] string emojiString,
        [Summary("description", "설명")] string description)
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

        try
        {
            await this.Context.Interaction.DeferAsync();
            
            var manager = await context.RoleManagers.FirstOrDefaultAsync(m => m.ServerId == server.Id && m.Id == managerId);
            if (manager == null)
            {
                throw new Exception("역할 관리 메시지를 찾지 못했습니다.");
            }
            
            var channel = this.Context.Guild.GetTextChannel(manager.ChannelId);
            if (channel == null)
            {
                throw new Exception($"{manager.ChannelId} 채널을 찾지 못했습니다.");
            }

            var emoji = ParseEmoji(this.Context.Guild, emojiString);

            if (emoji == null)
            {
                throw new Exception($"{emojiString} 이모지를 찾지 못했습니다.");
            }
            
            var encodedEmoji = StringToBytes(emojiString);

            var msg = await channel.GetMessageAsync(manager.MessageId);
            if (msg == null)
            {
                throw new Exception($"{manager.MessageId} 메시지를 찾지 못했습니다.");
            }

            var hasDuplicate = await context.Roles.AnyAsync(r => r.RoleManagerId == managerId && r.Emoji.SequenceEqual(encodedEmoji));
            if (hasDuplicate)
            {
                throw new Exception($"{managerId} - {emojiString} 이미 등록된 이모지입니다.");
            }

            hasDuplicate =
                await context.Roles.AnyAsync(r => r.RoleManagerId == managerId && r.DiscordRoleId == discordRole.Id);
            if (hasDuplicate)
            {
                throw new Exception($"{managerId} - {discordRole.Name} 이미 등록된 역할입니다.");
            }

            await msg.AddReactionAsync(emoji);

            var role = new Role
            {
                Description = description,
                DiscordRoleId = discordRole.Id,
                Emoji = encodedEmoji,
                EmojiForDebug = emojiString,
                RoleManagerId = managerId
            };
            await context.Roles.AddAsync(role);

            await context.SaveChangesAsync();
            await UpdateRoleMessage(channel, manager, await context.Roles.ToArrayAsync());
            await transaction.CommitAsync();
            
            await Log.Info($"Role 등록 : {manager.Title} - {role.Description} by {this.Context.User.Username}");
            await this.Context.Interaction.FollowupAsync("역할 등록에 성공했습니다.");
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            await Log.Fatal(e);
            await this.Context.Interaction.FollowupAsync($"역할 등록에 실패했습니다.\n{e.Message}", ephemeral: true);
        }
    }
    
    [SlashCommand("remove", "부여 가능한 역할을 삭제합니다.")]
    [RequireAdminPermission]
    public async Task RemoveRoleAsync(
        [Summary("manager", "역할 관리 메시지"), Autocomplete(typeof(RoleManagerAutocompleteHandler))] ulong managerId,
        [Summary("role", "역할")] IRole discordRole)
    {
        if (Global.Bot == null)
        {
            await this.Context.Interaction.RespondAsync("봇 정보 조회에 실패했습니다.");
            return;
        }
        
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

        try
        {
            await this.Context.Interaction.DeferAsync();
            
            var manager = await context.RoleManagers.FirstOrDefaultAsync(m => m.ServerId == server.Id && m.Id == managerId);
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

            var role = await context.Roles.FirstOrDefaultAsync(r =>
                r.RoleManagerId == managerId && r.DiscordRoleId == discordRole.Id);
            if (role == null)
            {
                throw new Exception($"{discordRole.Id} 역할을 찾지 못했습니다.");
            }

            var decodedEmoji = BytesToString(role.Emoji);

            var emoji = ParseEmoji(this.Context.Guild, decodedEmoji);
            if (emoji == null)
            {
                throw new Exception($"{decodedEmoji} 올바른 이모지가 아닙니다.");
            }
            await msg.RemoveReactionAsync(emoji, Global.Bot.Client.CurrentUser);
            context.Roles.Remove(role);

            await context.SaveChangesAsync();
            await UpdateRoleMessage(channel, manager, await context.Roles.ToArrayAsync());
            await transaction.CommitAsync();
            
            await Log.Info($"Role 삭제 : {manager.Title} - {role.Description} by {this.Context.User.Username}");
            await this.Context.Interaction.FollowupAsync("역할 삭제에 성공했습니다.");
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            await Log.Fatal(e);
            await this.Context.Interaction.FollowupAsync($"역할 삭제에 실패했습니다.\n{e.Message}", ephemeral: true);
        }
    }

    private static async Task UpdateRoleMessage(SocketTextChannel channel, RoleManager manager, ICollection<Role> roles)
    {
        var msg = await channel.GetMessageAsync(manager.MessageId);
        if (msg == null)
        {
            throw new Exception("역할 관리 메시지를 찾지 못했습니다.");
        }

        var embed = new EmbedBuilder()
            .WithTitle(manager.Title);

        if (!string.IsNullOrWhiteSpace(manager.Description))
        {
            embed.WithDescription(manager.Description);
        }

        foreach (var role in roles)
        {
            try
            {
                var decodedEmoji = BytesToString(role.Emoji);
                var emoji = ParseEmoji(channel.Guild, decodedEmoji);
                var discordRole = channel.Guild.GetRole(role.DiscordRoleId);
                embed.AddField($"{emoji} : {discordRole.Name}", role.Description ?? string.Empty, inline: true);
            }
            catch (Exception e)
            {
                await Log.Fatal(e);
            }
        }
        
        await channel.ModifyMessageAsync(msg.Id, properties =>
        {
            properties.Embed = embed.Build();
        });
    }

    private static IEmote? ParseEmoji(SocketGuild guild, string emojiString)
    {
        var emojiTokens = emojiString
            .Replace("<","")
            .Replace(">","")
            [1..]
            .Split(':');
            
        IEmote? emoji;

        if (emojiTokens.Length == 2 && ulong.TryParse(emojiTokens[1], out var emojiId))
        {
            emoji = guild.Emotes.FirstOrDefault(e => e.Name == emojiTokens[0] && e.Id == emojiId);
        }
        else
        {
            emoji = guild.Emotes.FirstOrDefault(e => e.Name == emojiString);
        }
        
        return emoji ?? Emoji.Parse(emojiString);
    }

    private static async Task OnReactionAdded(SocketReaction reaction)
    {
        // 반응을 한 유저가 봇이 아니어야 함
        if (!reaction.User.IsSpecified || reaction.User.Value.IsBot) return;

        // 채널 정보가 유효해야 함
        if (reaction.Channel is not SocketTextChannel channel)
        {
            return;
        }

        var emojiName = reaction.Emote.Name;
        if (reaction.Emote is Emote emote)
        {
            var found = channel.Guild.Emotes.FirstOrDefault(e => e.Id == emote.Id && e.Name == emote.Name);
            if (found != null)
            {
                emojiName = $"<:{found.Name}:{found.Id}>";
            }
        }
        
        await using var conn = await Database.Instance.CreateDbContextAsync();
        await using var transaction = await conn.Database.BeginTransactionAsync();

        try
        {
            var server = await conn.Servers
                .FirstOrDefaultAsync(s => s.DiscordUID == channel.Guild.Id);

            if (server == null)
            {
                await transaction.RollbackAsync();
                return;
            }

            var manager = await conn.RoleManagers
                .FirstOrDefaultAsync(m => m.ServerId == server.Id
                                          && m.ChannelId == channel.Id
                                          && m.MessageId == reaction.MessageId);

            if (manager == null)
            {
                await transaction.RollbackAsync();
                return;
            }

            var encodedEmoji = StringToBytes(emojiName);

            var role = await conn.Roles
                .FirstOrDefaultAsync(r => r.RoleManagerId == manager.Id && r.Emoji.SequenceEqual(encodedEmoji));

            if (role == null)
            {
                await transaction.RollbackAsync();
                return;
            }

            if (reaction.User.Value is not SocketGuildUser guildUser)
            {
                await transaction.RollbackAsync();
                return;
            }

            var discordRole = guildUser.Guild.GetRole(role.DiscordRoleId);
            if (discordRole == null)
            {
                await transaction.RollbackAsync();
                return;
            }

            await guildUser.AddRoleAsync(discordRole);
            await transaction.CommitAsync();
            await Log.Info($"역할 부여 : {guildUser.Username} | {discordRole.Name} | {emojiName}");
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            await Log.Fatal(e);
        }
    }
    
    private static async Task OnReactionRemoved(SocketReaction reaction)
    {
        // 반응을 한 유저가 봇이 아니어야 함
        if (!reaction.User.IsSpecified || reaction.User.Value.IsBot) return;

        // 채널 정보가 유효해야 함
        if (reaction.Channel is not SocketTextChannel channel)
        {
            return;
        }

        var emojiName = reaction.Emote.Name;
        if (reaction.Emote is Emote emote)
        {
            var found = channel.Guild.Emotes.FirstOrDefault(e => e.Id == emote.Id && e.Name == emote.Name);
            if (found != null)
            {
                emojiName = $"<:{found.Name}:{found.Id}>";
            }
        }
        
        await using var conn = await Database.Instance.CreateDbContextAsync();
        await using var transaction = await conn.Database.BeginTransactionAsync();

        try
        {
            var server = await conn.Servers
                .FirstOrDefaultAsync(s => s.DiscordUID == channel.Guild.Id);

            if (server == null)
            {
                await transaction.RollbackAsync();
                return;
            }

            var manager = await conn.RoleManagers
                .FirstOrDefaultAsync(m => m.ServerId == server.Id
                                          && m.ChannelId == channel.Id
                                          && m.MessageId == reaction.MessageId);

            if (manager == null)
            {
                await transaction.RollbackAsync();
                return;
            }

            var encodedEmoji = StringToBytes(emojiName);

            var role = await conn.Roles
                .FirstOrDefaultAsync(r => r.RoleManagerId == manager.Id && r.Emoji.SequenceEqual(encodedEmoji));

            if (role == null)
            {
                await transaction.RollbackAsync();
                return;
            }

            if (reaction.User.Value is not SocketGuildUser guildUser)
            {
                await transaction.RollbackAsync();
                return;
            }

            var discordRole = guildUser.Guild.GetRole(role.DiscordRoleId);
            if (discordRole == null)
            {
                await transaction.RollbackAsync();
                return;
            }

            await guildUser.RemoveRoleAsync(discordRole);
            await transaction.CommitAsync();
            await Log.Info($"역할 제거 : {guildUser.Username} | {discordRole.Name} | {emojiName}");
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            await Log.Fatal(e);
        }
    }

    private static byte[] StringToBytes(string str) => Encoding.UTF8.GetBytes(str);
    private static string BytesToString(byte[] bytes) => Encoding.UTF8.GetString(bytes);

    private class RoleManagerAutocompleteHandler : AutocompleteHandler
    {
        private const int ApiLimit = 25;
        
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter, IServiceProvider services)
        {
            await using var dbContext = await Database.Instance.CreateDbContextAsync();
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            try
            {
                var server = await dbContext.Servers.FirstOrDefaultAsync(s => s.DiscordUID == context.Guild.Id);
                if (server == null)
                {
                    throw new Exception("등록되지 않은 서버입니다.");
                }
                
                var managers = dbContext.RoleManagers
                    .Where(r => r.ServerId == server.Id)
                    .Select(r => new AutocompleteResult(r.Title, r.Id));

                return AutocompletionResult.FromSuccess(managers.Take(ApiLimit));
            }
            catch (Exception e)
            {
                return AutocompletionResult.FromError(e);
            }
        }
    }
}