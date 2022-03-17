using Discord;
using Discord.Commands;
using Discord.Commands.Builders;
using Discord.WebSocket;
using NursingBot.Features.Preconditions;

namespace NursingBot.Features
{
    [Group("party")]
    [RequireRegister]
    public class PartyModule : ModuleBase<SocketCommandContext>
    {
        private static readonly string STR_OK = "🇴";
        private static readonly string STR_NO = "🇽";
        private static readonly string STR_CLOSE = "🚫";
        private static readonly Emoji EMOJI_OK = Emoji.Parse(STR_OK);
        private static readonly Emoji EMOJI_NO = Emoji.Parse(STR_NO);
        private static readonly Emoji EMOJI_CLOSE = Emoji.Parse(STR_CLOSE);

        [Command("register")]
        [Summary("파티 모집 공고를 등록할 채널 설정")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task RegisterAsync([Summary("파티 모집 공고가 올라갈 채널입니다. #채널이름 형식으로 입력하시면 됩니다.")] ITextChannel channel)
        {
            await this.ReplyAsync(channel.Name);
        }

        [Command("add")]
        [Summary("파티 모집 공고 등록")]
        public async Task AddAsync([Remainder][Summary("설명과 예정 일시를 {설명};{예정 일시} 형태로 적어주시면 됩니다.")] string args)
        {
            if (this.Context.Channel is SocketTextChannel channel)
            {
                var tokens = args.Split(';');
                if (tokens.Length != 2)
                {
                    await this.ReplyAsync("형식이 올바르지 않습니다.");
                    return;
                }

                var (description, date) = (tokens[0], tokens[1]);

                var msg = await channel.SendMessageAsync(embed: Build(this.Context.User, description, date, new[] { this.Context.User }));
                await msg.AddReactionsAsync(new[]
                {
                    EMOJI_OK, EMOJI_NO, EMOJI_CLOSE
                });

                await channel.CreateThreadAsync("파티 스레드", message: msg);
            }
        }

        private static Embed Build(SocketUser sender, string description, string date, SocketUser[] users)
        {
            return new EmbedBuilder()
                .WithTitle("파티 모집")
                .WithDescription($"{sender.Mention} 님이 등록한 모집 공고입니다.")
                .AddField("설명", description)
                .AddField("예정 일시", date)
                .AddField("참가자 목록", string.Join(", ", users.Select(u => u.Username)))
                .AddField("참가 여부", $"{STR_OK} : 참가\n{STR_NO} : 불참\n{STR_CLOSE} : 마감")
                .Build();
        }

        private static async Task OnReactionAdded(Cacheable<IUserMessage, ulong> _, Cacheable<IMessageChannel, ulong> __, SocketReaction reaction)
        {
            var channel = reaction.Channel;

            // 채널 정보가 유효해야 함
            if (channel == null) return;

            // 반응을 한 유저가 봇이 아니어야 함
            if (!reaction.User.IsSpecified || reaction.User.Value.IsBot) return;
            
            await channel.SendMessageAsync(reaction.MessageId.ToString());
        }

        private static async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> _, Cacheable<IMessageChannel, ulong> __, SocketReaction reaction)
        {
            var channel = reaction.Channel;

            // 채널 정보가 유효해야 함
            if (channel == null) return;

            // 반응을 한 유저가 봇이 아니어야 함
            if (!reaction.User.IsSpecified || reaction.User.Value.IsBot) return;
            
            await channel.SendMessageAsync(reaction.MessageId.ToString());
        }

        protected override void OnModuleBuilding(CommandService commandService, ModuleBuilder builder)
        {
            base.OnModuleBuilding(commandService, builder);

            Global.Bot!.Client.ReactionAdded += OnReactionAdded;
            Global.Bot!.Client.ReactionRemoved += OnReactionRemoved;
        }
    }
}