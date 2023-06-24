using Discord;
using Discord.Interactions;

namespace NursingBot.Feature.Preconditions;

public class RequireAdminPermissionAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo command, IServiceProvider services)
    {
        if (context.User is IGuildUser user)
        {
            if (user.GuildPermissions.Administrator || user.Id == Global.SuperUserId)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
        }

        return Task.FromResult(PreconditionResult.FromError("이 명령을 사용할 권한이 없습니다!"));
    }
}