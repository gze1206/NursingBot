using Discord.Commands;
using Discord.WebSocket;

namespace NursingBot.Feature.Preconditions
{
    public class RequireAdminPermissionAttribute : RequireUserPermissionAttribute
    {
        public RequireAdminPermissionAttribute() : base(Discord.GuildPermission.Administrator)
        { }

#pragma warning disable
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
#pragma warning enable
        {
            if (context.User is SocketGuildUser user)
            {
                if (user.Id == Global.SuperUserId)
                {
                    return PreconditionResult.FromSuccess();
                }
            }

            return await base.CheckPermissionsAsync(context, command, services);
        }
    }
}
