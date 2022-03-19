using Discord.Commands;
using Discord.WebSocket;
using NursingBot.Core;
using NursingBot.Models;

namespace NursingBot.Features.Preconditions
{
    public class RequireRegisterAttribute : PreconditionAttribute
    {
        #pragma warning disable
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        #pragma warning enable
        {
            if (context.User is SocketGuildUser user)
            {
                if (Database.CachedServers.ContainsKey(user.Guild.Id))
                {
                    return PreconditionResult.FromError("등록되지 않은 서버입니다!\nregister 명령을 통해 서버를 등록해주세요.");
                }
            }
            return PreconditionResult.FromSuccess();
        }
    }
}