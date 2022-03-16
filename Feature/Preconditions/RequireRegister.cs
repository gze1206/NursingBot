using Discord.Commands;
using Discord.WebSocket;
using NursingBot.Core;

namespace NursingBot.Features.Preconditions
{
    public class RequireRegisterAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.User is SocketGuildUser user)
            {
                using var conn = await Database.Open();
                using var cmd = conn.CreateCommand();

                cmd.CommandText = $"SELECT * FROM servers WHERE discordUID={user.Guild.Id};";
                var reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();
                if (!reader.HasRows)
                {
                    return PreconditionResult.FromError("등록되지 않은 서버입니다!\nregister 명령을 통해 서버를 등록해주세요.");
                }
            }
            return PreconditionResult.FromSuccess();
        }
    }
}