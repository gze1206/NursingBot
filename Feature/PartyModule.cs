using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NursingBot.Core;
using NursingBot.Feature.Preconditions;
using NursingBot.Features.Preconditions;
using NursingBot.Logger;
using NursingBot.Models;

namespace NursingBot.Features
{
    [Group("party", "특정 목적을 가지고 사람을 모으기 위한 파티 모집 기능입니다.")]
    [RequireRegister]
    public class PartyModule : InteractionModuleBase<SocketInteractionContext>
    {
        private static readonly string STR_OK = "🇴";
        private static readonly string STR_NO = "🇽";
        private static readonly string STR_CLOSE = "🚫";
        private static readonly Emoji EMOJI_OK = Emoji.Parse(STR_OK);
        private static readonly Emoji EMOJI_NO = Emoji.Parse(STR_NO);
        private static readonly Emoji EMOJI_CLOSE = Emoji.Parse(STR_CLOSE);

        private static readonly string[] DetectingReactions = new[]
        {
            STR_OK, STR_CLOSE
        };

        [SlashCommand("register", "파티 모집 공고를 등록할 채널 설정 / 변경")]
        [RequireAdminPermission]
        public async Task RegisterAsync([Summary("channel", "파티 모집 공고가 올라갈 채널입니다.")] ITextChannel channel)
        {
            if (!Database.CachedServers.TryGetValue(channel.GuildId, out var server))
            {
                await this.Context.Interaction.RespondAsync("서버 정보 조회에 실패했습니다...", ephemeral: true);
                return;
            }

            using var context = await Database.Instance.CreateDbContextAsync();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var partyChannel = await context.PartyChannels.FirstOrDefaultAsync(partyChannel =>
                    partyChannel.ServerId == server.Id);

                if (partyChannel == null)
                {
                    partyChannel = new PartyChannel
                    {
                        ServerId = server.Id,
                    };
                }

                partyChannel.ChannelId = channel.Id;

                if (partyChannel.Id != 0)
                {
                    context.PartyChannels.Update(partyChannel);
                    await context.SaveChangesAsync();
                    await this.Context.Interaction.RespondAsync("파티 모집 채널 재설정에 성공했습니다!");
                }
                else
                {
                    await context.PartyChannels.AddAsync(partyChannel);
                    await context.SaveChangesAsync();
                    await this.Context.Interaction.RespondAsync("파티 모집 채널 설정에 성공했습니다!");
                }

                await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                await Log.Fatal(e);
                await this.Context.Interaction.RespondAsync("파티 모집 채널 설정에 실패했습니다...", ephemeral: true);
            }
        }

        [SlashCommand("add", "파티 모집 공고 등록")]
        public async Task AddAsync([Summary("description", "무슨 파티에서 구인을 하는 것인지에 대한 설명")] string description, [Summary("date", "파티가 행동을 할 예정 일시")] string date)
        {
            if (this.Context.Channel is SocketTextChannel channel)
            {

                if (!Database.CachedServers.TryGetValue(channel.Guild.Id, out var server))
                {
                    await this.Context.Interaction.RespondAsync("서버 정보 조회에 실패했습니다...", ephemeral: true);
                    return;
                }

                if (string.IsNullOrWhiteSpace(description))
                {
                    description = "(설명 없음)";
                }
                if (string.IsNullOrWhiteSpace(date))
                {
                    date = "(미정)";
                }

                using var context = await Database.Instance.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    var partyChannel = await context.PartyChannels
                        .FirstOrDefaultAsync(p => p.ServerId == server.Id);
                    
                    if (partyChannel == null)
                    {
                        throw new NullReferenceException("파티 모집 공고를 올릴 채널이 설정되지 않았습니다.\nparty register 명령을 통해 등록해주세요.");
                    }

                    var targetChannel = channel.Guild.GetTextChannel(partyChannel.ChannelId);

                    var msg = await targetChannel.SendMessageAsync(embed: Build(this.Context.User, description, date, Array.Empty<IUser>()));

                    var recruit = new PartyRecruit
                    {
                        PartyChannelId = partyChannel.Id,
                        MessageId = msg.Id,
                        AuthorId = this.Context.User.Id,
                        Description = description,
                        Date = date,
                    };

                    await this.Context.Interaction.DeferAsync();

                    await msg.AddReactionsAsync(new[]
                    {
                        EMOJI_OK, EMOJI_NO, EMOJI_CLOSE
                    });

                    await targetChannel.CreateThreadAsync("파티 스레드", message: msg);

                    await context.PartyRecruits.AddAsync(recruit);
                    await context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    await this.Context.Interaction.FollowupAsync("파티 모집 공고를 등록했습니다!");
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    await Log.Fatal(e);
                    await this.Context.Interaction.FollowupAsync($"모집 공고 등록에 실패했습니다...\n{e.Message}", ephemeral: true);
                    return;
                }

            }
        }

        private static Embed Build(IUser sender, string? description, string? date, IUser[] users, bool isClosed = false)
        {
            var memberText = string.Empty;

            if (users.Length > 0)
            {
                memberText = string.Join(", ", users
                    .DistinctBy(u => u.Id)
                    .Select(u => (u as SocketGuildUser)?.DisplayName ?? u.Username));
            }

            if (string.IsNullOrWhiteSpace(memberText))
            {
                memberText = "(없음)";
            }

            return new EmbedBuilder()
                .WithTitle(isClosed ? "*[마감됨]* 파티 모집" : "파티 모집")
                .WithDescription($"{sender?.Mention ?? "<UNKNOWN>"} 님이 등록한 모집 공고입니다.")
                .AddField("설명", description)
                .AddField("예정 일시", date)
                .AddField("참가자 목록", memberText)
                .AddField("참가 여부", $"{STR_OK} : 참가\n{STR_NO} : 불참\n{STR_CLOSE} : 마감")
                .Build();
        }

        private static PartyRecruit? GetPartyRecruit(ApplicationDbContext context, Server server, ulong channelId, ulong messageId)
        {
            
            var partyChannel = context.PartyChannels
                .FirstOrDefault(p => p.ServerId == server.Id && p.ChannelId == channelId);

            if (partyChannel == null)
            {
                return null;
            }

            var recruit = context.PartyRecruits
                .FirstOrDefault(p => p.PartyChannelId == partyChannel.Id && p.MessageId == messageId);

            return recruit;
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

            // 참가나 마감이 아니면 반응할 필요 없음
            var emojiName = reaction.Emote.Name;
            if (!DetectingReactions.Contains(emojiName))
            {
                return;
            }

            using var context = await Database.Instance.CreateDbContextAsync();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var server = await context.Servers
                    .FirstOrDefaultAsync(s => s.DiscordUID == channel.Guild.Id);
                
                if (server == null)
                {
                    await transaction.RollbackAsync();
                    return;
                }

                var recruit = GetPartyRecruit(context, server, channel.Id, reaction.MessageId);
                if (recruit == null || recruit.IsClosed)
                {
                    await transaction.RollbackAsync();
                    return;
                }

                var author = channel.GetUser(recruit.AuthorId);
                var msg = await channel.GetMessageAsync(recruit.MessageId);

                if (emojiName.Equals(STR_OK))
                {
                    var member = await msg.GetReactionUsersAsync(EMOJI_OK, int.MaxValue)
                        .Flatten()
                        .Where(u => !u.IsBot)
                        .Select(u => channel.Guild.GetUser(u.Id))
                        .ToArrayAsync();

                    var embed = Build(author, recruit.Description, recruit.Date, member);

                    await channel.ModifyMessageAsync(msg.Id, p => p.Embed = embed);

                    recruit.UpdatedAt = DateTime.UtcNow;
                    context.PartyRecruits.Update(recruit);
                    await context.SaveChangesAsync();
                }
                else if (emojiName.Equals(STR_CLOSE))
                {
                    if (recruit.IsClosed)
                    {
                        await transaction.RollbackAsync();
                        return;
                    }

                    var member = await msg.GetReactionUsersAsync(EMOJI_OK, int.MaxValue)
                        .Flatten()
                        .Where(u => !u.IsBot)
                        .Select(u => channel.Guild.GetUser(u.Id))
                        .ToArrayAsync();

                    var embed = Build(author, recruit.Description, recruit.Date, member, true);
                    
                    await channel.ModifyMessageAsync(msg.Id, p => p.Embed = embed);

                    recruit.IsClosed = true;
                    recruit.UpdatedAt = DateTime.UtcNow;
                    recruit.ClosedAt = DateTime.UtcNow;
                    context.PartyRecruits.Update(recruit);
                    await context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
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

            // 참가나 마감이 아니면 반응할 필요 없음
            var emojiName = reaction.Emote.Name;
            if (!DetectingReactions.Contains(emojiName))
            {
                return;
            }

            using var context = await Database.Instance.CreateDbContextAsync();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var server = await context.Servers
                    .FirstOrDefaultAsync(s => s.DiscordUID == channel.Guild.Id);
                
                if (server == null)
                {
                    await transaction.RollbackAsync();
                    return;
                }

                var recruit = GetPartyRecruit(context, server, channel.Id, reaction.MessageId);
                if (recruit == null)
                {
                    await transaction.RollbackAsync();
                    return;
                }

                var author = channel.GetUser(recruit.AuthorId);
                var msg = await channel.GetMessageAsync(recruit.MessageId);

                if (emojiName.Equals(STR_OK))
                {
                    if (recruit.IsClosed)
                    {
                        await transaction.RollbackAsync();
                        return;
                    }

                    var member = await msg.GetReactionUsersAsync(EMOJI_OK, int.MaxValue)
                        .Flatten()
                        .Where(u => !u.IsBot)
                        .Select(u => channel.Guild.GetUser(u.Id))
                        .ToArrayAsync();

                    var embed = Build(author, recruit.Description, recruit.Date, member);

                    await channel.ModifyMessageAsync(msg.Id, p => p.Embed = embed);

                    recruit.UpdatedAt = DateTime.UtcNow;
                    context.PartyRecruits.Update(recruit);
                    await context.SaveChangesAsync();
                }
                else if (emojiName.Equals(STR_CLOSE))
                {
                    var isClose = await msg.GetReactionUsersAsync(EMOJI_CLOSE, int.MaxValue)
                        .Flatten()
                        .Where(u => !u.IsBot)
                        .CountAsync() > 0;

                    if (isClose == recruit.IsClosed)
                    {
                        await transaction.RollbackAsync();
                        return;
                    }

                    var member = await msg.GetReactionUsersAsync(EMOJI_OK, int.MaxValue)
                        .Flatten()
                        .Where(u => !u.IsBot)
                        .Select(u => channel.Guild.GetUser(u.Id))
                        .ToArrayAsync();

                    var embed = Build(author, recruit.Description, recruit.Date, member, isClose);

                    await channel.ModifyMessageAsync(msg.Id, p => p.Embed = embed);

                    recruit.UpdatedAt = DateTime.UtcNow;
                    if (!isClose)
                    {
                        recruit.IsClosed = isClose;
                    }
                    context.PartyRecruits.Update(recruit);
                    await context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                await Log.Fatal(e);
            }
        }

        public override void OnModuleBuilding(InteractionService commandService, ModuleInfo moduleinfo)
        {
            base.OnModuleBuilding(commandService, moduleinfo);

            Global.Bot!.Client.ReactionAdded += (_, _, reaction) =>
            {
                _ = OnReactionAdded(reaction);
                return Task.CompletedTask;
            };

            Global.Bot!.Client.ReactionRemoved += (_, _, reaction) =>
            {
                _ = OnReactionRemoved(reaction);
                return Task.CompletedTask;
            };
        }
    }
}