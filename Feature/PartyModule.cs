using Discord;
using Discord.Commands;
using Discord.Commands.Builders;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NursingBot.Core;
using NursingBot.Feature.Preconditions;
using NursingBot.Features.Preconditions;
using NursingBot.Logger;
using NursingBot.Models;

namespace NursingBot.Features
{
    [Group("party")]
    [RequireRegister]
    public class PartyModule : ModuleBase<SocketCommandContext>
    {
        private static readonly string STR_OK = "π΄";
        private static readonly string STR_NO = "π½";
        private static readonly string STR_CLOSE = "π«";
        private static readonly Emoji EMOJI_OK = Emoji.Parse(STR_OK);
        private static readonly Emoji EMOJI_NO = Emoji.Parse(STR_NO);
        private static readonly Emoji EMOJI_CLOSE = Emoji.Parse(STR_CLOSE);

        private static readonly string[] DetectingReactions = new[]
        {
            STR_OK, STR_CLOSE
        };

        [Command("register")]
        [Summary("νν° λͺ¨μ§ κ³΅κ³ λ₯Ό λ±λ‘ν  μ±λ μ€μ  / λ³κ²½")]
        [RequireAdminPermission]
        public async Task RegisterAsync([Summary("νν° λͺ¨μ§ κ³΅κ³ κ° μ¬λΌκ° μ±λμλλ€. #μ±λμ΄λ¦ νμμΌλ‘ μλ ₯νμλ©΄ λ©λλ€.")] ITextChannel channel)
        {
            if (!Database.CachedServers.TryGetValue(channel.GuildId, out var server))
            {
                await this.Context.Message.ReplyAsync("μλ² μ λ³΄ μ‘°νμ μ€ν¨νμ΅λλ€...");
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
                    await this.Context.Message.ReplyAsync("νν° λͺ¨μ§ μ±λ μ¬μ€μ μ μ±κ³΅νμ΅λλ€!");
                }
                else
                {
                    await context.PartyChannels.AddAsync(partyChannel);
                    await context.SaveChangesAsync();
                    await this.Context.Message.ReplyAsync("νν° λͺ¨μ§ μ±λ μ€μ μ μ±κ³΅νμ΅λλ€!");
                }
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                await Log.Fatal(e);
                await this.Context.Message.ReplyAsync("νν° λͺ¨μ§ μ±λ μ€μ μ μ€ν¨νμ΅λλ€...");
            }
        }

        [Command("add")]
        [Summary("νν° λͺ¨μ§ κ³΅κ³  λ±λ‘")]
        public async Task AddAsync([Remainder][Summary("μ€λͺκ³Ό μμ  μΌμλ₯Ό {μ€λͺ};{μμ  μΌμ} ννλ‘ μ μ΄μ£Όμλ©΄ λ©λλ€.")] string args)
        {
            if (this.Context.Channel is SocketTextChannel channel)
            {
                var tokens = args.Split(';');
                if (tokens.Length != 2)
                {
                    await this.ReplyAsync("νμμ΄ μ¬λ°λ₯΄μ§ μμ΅λλ€.");
                    return;
                }

                if (!Database.CachedServers.TryGetValue(channel.Guild.Id, out var server))
                {
                    await this.Context.Message.ReplyAsync("μλ² μ λ³΄ μ‘°νμ μ€ν¨νμ΅λλ€...");
                    return;
                }

                var (description, date) = (tokens[0], tokens[1]);

                if (string.IsNullOrWhiteSpace(description))
                {
                    description = "(μ€λͺ μμ)";
                }
                if (string.IsNullOrWhiteSpace(date))
                {
                    date = "(λ―Έμ )";
                }

                using var context = await Database.Instance.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    var partyChannel = await context.PartyChannels
                        .FirstOrDefaultAsync(p => p.ServerId == server.Id);
                    
                    if (partyChannel == null)
                    {
                        throw new NullReferenceException("νν° λͺ¨μ§ κ³΅κ³ λ₯Ό μ¬λ¦΄ μ±λμ΄ μ€μ λμ§ μμμ΅λλ€.\nparty register λͺλ Ήμ ν΅ν΄ λ±λ‘ν΄μ£ΌμΈμ.");
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

                    await msg.AddReactionsAsync(new[]
                    {
                        EMOJI_OK, EMOJI_NO, EMOJI_CLOSE
                    });

                    await targetChannel.CreateThreadAsync("νν° μ€λ λ", message: msg);

                    await context.PartyRecruits.AddAsync(recruit);
                    await context.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    await Log.Fatal(e);
                    await this.Context.Message.ReplyAsync($"λͺ¨μ§ κ³΅κ³  λ±λ‘μ μ€ν¨νμ΅λλ€...\n{e.Message}");
                    return;
                }

                await this.Context.Message.ReplyAsync("νν° λͺ¨μ§ κ³΅κ³ λ₯Ό λ±λ‘νμ΅λλ€!");
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
                memberText = "(μμ)";
            }

            return new EmbedBuilder()
                .WithTitle(isClosed ? "*[λ§κ°λ¨]* νν° λͺ¨μ§" : "νν° λͺ¨μ§")
                .WithDescription($"{sender?.Mention ?? "<UNKNOWN>"} λμ΄ λ±λ‘ν λͺ¨μ§ κ³΅κ³ μλλ€.")
                .AddField("μ€λͺ", description)
                .AddField("μμ  μΌμ", date)
                .AddField("μ°Έκ°μ λͺ©λ‘", memberText)
                .AddField("μ°Έκ° μ¬λΆ", $"{STR_OK} : μ°Έκ°\n{STR_NO} : λΆμ°Έ\n{STR_CLOSE} : λ§κ°")
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
            // λ°μμ ν μ μ κ° λ΄μ΄ μλμ΄μΌ ν¨
            if (!reaction.User.IsSpecified || reaction.User.Value.IsBot) return;

            // μ±λ μ λ³΄κ° μ ν¨ν΄μΌ ν¨
            if (reaction.Channel is not SocketTextChannel channel)
            {
                return;
            }

            // μ°Έκ°λ λ§κ°μ΄ μλλ©΄ λ°μν  νμ μμ
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
            // λ°μμ ν μ μ κ° λ΄μ΄ μλμ΄μΌ ν¨
            if (!reaction.User.IsSpecified || reaction.User.Value.IsBot) return;

            // μ±λ μ λ³΄κ° μ ν¨ν΄μΌ ν¨
            if (reaction.Channel is not SocketTextChannel channel)
            {
                return;
            }

            // μ°Έκ°λ λ§κ°μ΄ μλλ©΄ λ°μν  νμ μμ
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
        }
    }
}