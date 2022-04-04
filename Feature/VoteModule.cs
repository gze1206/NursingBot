using Discord;
using Discord.Commands;
using Discord.Commands.Builders;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NursingBot.Core;
using NursingBot.Features.Preconditions;
using NursingBot.Logger;
using NursingBot.Models;
using System.Text;

namespace NursingBot.Feature
{
    [RequireRegister]
    public class VoteModule : ModuleBase<SocketCommandContext>
    {
        private const int MAX_REACTIONS = 19;

        private static readonly List<string> emojiStringList = new();
        private static readonly List<Emoji> emojiList = new();
        private static readonly string STR_CLOSE = "🚫";
        private static readonly Emoji EMOJI_CLOSE = Emoji.Parse(STR_CLOSE);

        protected override void OnModuleBuilding(CommandService commandService, ModuleBuilder builder)
        {
            base.OnModuleBuilding(commandService, builder);

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

            for (int i = 0x01F1E6, max = 0x01F1FF; i <= max; i++)
            {
                var str = char.ConvertFromUtf32(i);
                emojiStringList.Add(str);
                emojiList.Add(Emoji.Parse(str));
            }
        }

        [Command("vote")]
        public async Task VoteAsync([Remainder] string args)
        {
            if (Context.Channel is not SocketTextChannel channel)
            {
                return;
            }

            if (!Database.CachedServers.TryGetValue(channel.Guild.Id, out var server))
            {
                await this.Context.Message.ReplyAsync("서버 정보 조회에 실패했습니다...");
                return;
            }

            var tokens = args.Split(';');

            if (tokens.Length < 2)
            {
                await ReplyAsync("투표 항목이 1개 이상 있어야 합니다.");
                return;
            }

            var description = tokens[0];
            var choices = tokens.Skip(1).ToArray();

            if (choices.Length > MAX_REACTIONS)
            {
                await ReplyAsync($"디스코드 정책 상 투표 항목은 최대 {MAX_REACTIONS}까지만 가능합니다.");
                return;
            }

            var embed = Build(this.Context.User, description, choices, null, false);

            var msg = await channel.SendMessageAsync(embed: embed);
            await msg.AddReactionsAsync(emojiList
                .Take(choices.Length)
                .Append(EMOJI_CLOSE));

            using var conn = await Database.Instance.CreateDbContextAsync();
            using var transaction = await conn.Database.BeginTransactionAsync();

            try
            {
                var vote = new Vote
                {
                    ServerId = server.Id,
                    AuthorId = this.Context.User.Id,
                    MessageId = msg.Id,
                    Description = description,
                    Choices = string.Join(';', choices),
                };

                await conn.Votes.AddAsync(vote);
                await conn.SaveChangesAsync();

                await transaction.CommitAsync();
                await this.Context.Message.ReplyAsync("투표가 준비되었습니다.");
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                await Log.Fatal(e);
                await this.Context.Message.ReplyAsync($"투표 등록에 실패했습니다...\n{e.Message}");
            }
            
        }

        private static Embed Build(IUser author, Vote vote, Dictionary<string, IUser[]>? usersPerEmoji)
        {
            return Build(author,
                         vote.Description,
                         vote.Choices?.Split(';') ?? Array.Empty<string>(),
                         usersPerEmoji,
                         vote.IsClosed);
        }

        private static Embed Build(IUser author, string? description, string[] choices, Dictionary<string, IUser[]>? usersPerEmoji, bool isClosed)
        {
            var builder = new EmbedBuilder()
                .WithTitle(isClosed ? "*[마감됨] 투표*" : "투표")
                .WithDescription($"{author?.Mention ?? "<UNKNOWN>"} 님이 개시한 투표입니다.");

            if (string.IsNullOrWhiteSpace(description))
            {
                description = "*(설명 없음)*";
            }
            builder.AddField("설명", description);

            var sb = new StringBuilder();
            for (int i = 0, max = choices.Length; i < max; i++)
            {
                var str = emojiStringList[i];
                var memberText = string.Empty;

                if (
                    (usersPerEmoji?.TryGetValue(str, out var users) ?? false)
                    && users.Length > 0)
                {
                    memberText = string.Join(", ", users
                        .DistinctBy(u => u.Id)
                        .Select(u => (u as SocketGuildUser)?.DisplayName ?? u.Username));
                }

                if (string.IsNullOrWhiteSpace(memberText))
                {
                    memberText = "(없음)";
                }

                sb.AppendLine($"**{choices[i]}** : {emojiList[i]}");
                sb.AppendLine(memberText);
                sb.AppendLine();
            }
            builder.AddField("투표 항목", sb.ToString());

            builder.AddField("투표 마감", $"{STR_CLOSE}를 눌러 투표를 마감할 수 있습니다.");

            return builder.Build();
        }

        private static Vote? GetVote(ApplicationDbContext context, Server server, ulong channelId, ulong messageId)
        {
            return context.Votes
                .FirstOrDefault(v =>
                    v.ServerId == server.Id && v.MessageId == messageId
                );
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
            using var conn = await Database.Instance.CreateDbContextAsync();
            using var transaction = await conn.Database.BeginTransactionAsync();

            try
            {
                var server = await conn.Servers
                    .FirstOrDefaultAsync(s => s.DiscordUID == channel.Guild.Id);

                if (server == null)
                {
                    await transaction.RollbackAsync();
                    return;
                }

                var vote = GetVote(conn, server, channel.Id, reaction.MessageId);

                if (vote == null)
                {
                    await transaction.RollbackAsync();
                    return;
                }

                if (vote.IsClosed)
                {
                    await transaction.RollbackAsync();
                    return;
                }

                var choices = vote.Choices?.Split(';') ?? Array.Empty<string>();
                var emojis = emojiList.Take(choices.Length).ToArray();
                var emojiStrings = emojiStringList.Take(choices.Length).ToArray();

                if (!emojiName.Equals(STR_CLOSE) && !emojiStrings.Contains(emojiName))
                {
                    await transaction.RollbackAsync();
                    return;
                }

                var author = channel.GetUser(vote.AuthorId);
                var msg = await channel.GetMessageAsync(vote.MessageId);

                var usersPerEmoji = new Dictionary<string, IUser[]>();
                for (int i = 0, max = choices.Length; i < max; i++)
                {
                    var members = await msg.GetReactionUsersAsync(emojis[i], int.MaxValue)
                        .Flatten()
                        .Where(u => !u.IsBot)
                        .Select(u => channel.Guild.GetUser(u.Id))
                        .ToArrayAsync();

                    usersPerEmoji[emojiStrings[i]] = members;
                }

                if (emojiName.Equals(STR_CLOSE))
                {
                    if (vote.IsClosed)
                    {
                        await transaction.RollbackAsync();
                        return;
                    }

                    vote.IsClosed = true;
                    vote.ClosedAt = DateTime.UtcNow;
                    vote.UpdatedAt = DateTime.UtcNow;
                    conn.Votes.Update(vote);
                    await conn.SaveChangesAsync();

                    var embed = Build(author, vote, usersPerEmoji);
                    await channel.ModifyMessageAsync(vote.MessageId, m => m.Embed = embed);
                }
                else
                {
                    vote.UpdatedAt = DateTime.UtcNow;
                    conn.Votes.Update(vote);
                    await conn.SaveChangesAsync();

                    var embed = Build(author, vote, usersPerEmoji);
                    await channel.ModifyMessageAsync(vote.MessageId, m => m.Embed = embed);
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

            var emojiName = reaction.Emote.Name;
            using var conn = await Database.Instance.CreateDbContextAsync();
            using var transaction = await conn.Database.BeginTransactionAsync();

            try
            {
                var server = await conn.Servers
                    .FirstOrDefaultAsync(s => s.DiscordUID == channel.Guild.Id);

                if (server == null)
                {
                    await transaction.RollbackAsync();
                    return;
                }

                var vote = GetVote(conn, server, channel.Id, reaction.MessageId);

                if (vote == null)
                {
                    await transaction.RollbackAsync();
                    return;
                }

                var choices = vote.Choices?.Split(';') ?? Array.Empty<string>();
                var emojis = emojiList.Take(choices.Length).ToArray();
                var emojiStrings = emojiStringList.Take(choices.Length).ToArray();

                if (!emojiName.Equals(STR_CLOSE) && !emojiStrings.Contains(emojiName))
                {
                    await transaction.RollbackAsync();
                    return;
                }

                var author = channel.GetUser(vote.AuthorId);
                var msg = await channel.GetMessageAsync(vote.MessageId);

                var usersPerEmoji = new Dictionary<string, IUser[]>();
                for (int i = 0, max = choices.Length; i < max; i++)
                {
                    var members = await msg.GetReactionUsersAsync(emojis[i], int.MaxValue)
                        .Flatten()
                        .Where(u => !u.IsBot)
                        .Select(u => channel.Guild.GetUser(u.Id))
                        .ToArrayAsync();

                    usersPerEmoji[emojiStrings[i]] = members;
                }

                if (emojiName.Equals(STR_CLOSE))
                {
                    var isClose = await msg.GetReactionUsersAsync(EMOJI_CLOSE, int.MaxValue)
                        .Flatten()
                        .Where(u => !u.IsBot)
                        .CountAsync() > 0;

                    if (isClose == vote.IsClosed)
                    {
                        await transaction.RollbackAsync();
                        return;
                    }

                    vote.UpdatedAt = DateTime.UtcNow;
                    if (!isClose)
                    {
                        vote.IsClosed = false;
                        vote.ClosedAt = null;
                    }
                    conn.Votes.Update(vote);
                    await conn.SaveChangesAsync();

                    var embed = Build(author, vote, usersPerEmoji);
                    await channel.ModifyMessageAsync(vote.MessageId, m => m.Embed = embed);
                }
                else
                {
                    if (vote.IsClosed)
                    {
                        await transaction.RollbackAsync();
                        return;
                    }

                    vote.UpdatedAt = DateTime.UtcNow;
                    conn.Votes.Update(vote);
                    await conn.SaveChangesAsync();

                    var embed = Build(author, vote, usersPerEmoji);
                    await channel.ModifyMessageAsync(vote.MessageId, m => m.Embed = embed);
                }

                await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                await Log.Fatal(e);
            }
        }
    }
}
