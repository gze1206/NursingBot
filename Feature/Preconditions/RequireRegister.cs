using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using NursingBot.Core;

namespace NursingBot.Features.Preconditions
{
    public class RequireRegisterAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo command, IServiceProvider services)
        {
            if (context.User is SocketGuildUser user)
            {
                if (!Database.CachedServers.ContainsKey(user.Guild.Id))
                {
                    return Task.FromResult(PreconditionResult.FromError("등록되지 않은 서버입니다!\nregister 명령을 통해 서버를 등록해주세요."));
                }
            }
            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}