using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NursingBot.Core;
using NursingBot.Logger;

namespace NursingBot.Feature.Preconditions;

public class RequireRegisterAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo command, IServiceProvider services)
    {
        if (context.User is SocketGuildUser user)
        {
            var guildId = user.Guild.Id;

            if (!Database.CachedServers.ContainsKey(guildId))
            {
                try
                {
                    await using var dbContext = await Database.Instance.CreateDbContextAsync();

                    var server = await dbContext.Servers
                        .Where(s => s.DiscordUID == guildId)
                        .FirstOrDefaultAsync();

                    if (server == null)
                    {
                        Database.ClearCache(guildId);
                        return PreconditionResult.FromError("등록되지 않은 서버입니다!\nregister 명령을 통해 서버를 등록해주세요.");
                    }
                    else
                    {
                        Database.Cache(guildId, server);
                    }
                }
                catch (Exception ex)
                {
                    await Log.Fatal(ex);
                }
            }
        }
        return PreconditionResult.FromSuccess();
    }
}