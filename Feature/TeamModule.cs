using Discord;
using Discord.Interactions;
using NursingBot.Core;
using NursingBot.Feature.Preconditions;
using NursingBot.Logger;

namespace NursingBot.Feature;

[RequireRegister]
public class TeamModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("team", "무작위로 팀을 나눕니다")]
    public async Task Build(
        [Summary("teams", "팀 형식을 입력해주세요. ex - 5v5, 2v2v2v2v2. 기본값 5v5")]
        string? teamStr = null,
        [Summary("groups", "찢어져야 할 인원을 그룹으로 묶습니다. 0으로 설정하면 그룹 없이 전체를 대상으로 무작위로 팀을 편성합니다. 기본값 0")]
        uint? groups = null,
        [Summary("autoWard", "이 값을 true로 설정하면 팀 확정 시 해당 인원을 자동으로 병실을 만들어 배분합니다.")]
        bool? autoWard = null)
    {
        if (!Database.CachedServers.TryGetValue(this.Context.Guild.Id, out var server))
        {
            await this.Context.Interaction.RespondAsync("서버 정보 조회에 실패했습니다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(teamStr))
        {
            teamStr = "5v5";
        }

        groups ??= 0;
        autoWard ??= false;

        await using var context = await Database.Instance.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            await this.Context.Interaction.DeferAsync();
            
            var (teams, totalMembers) = ParseTeamStr(teamStr);

            if (totalMembers < groups)
            {
                await this.Context.Interaction.RespondAsync(
                    "전체 멤버의 수보다 그룹의 수가 많을 수 없습니다. 모두가 공평하게 분배되길 원한다면 그룹수를 0으로 설정하세요.");
                return;
            }

            await transaction.CommitAsync();

            var embed = new EmbedBuilder()
                .WithTitle("팀 추첨")
                .WithDescription("찢어져야 하는 인원은 같은 그룹을 선택해주세요");

            var buttons = new ComponentBuilder();
            for (var i = 1; i <= groups; i++)
            {
                buttons.WithButton($"{i}번 그룹", i.ToString());
            }

            buttons.WithButton("추첨 종료", "quit", ButtonStyle.Danger, row: 1);
            buttons.WithButton("재추첨", "shuffle", ButtonStyle.Secondary, row: 1);
            buttons.WithButton("팀 확정", "submit", ButtonStyle.Success, row: 1);
            
            await this.Context.Interaction.FollowupAsync(embed: embed.Build(), components: buttons.Build());
        }
        catch (Exception e)
        {
            await Log.Fatal(e);
            await transaction.RollbackAsync();
            await this.Context.Interaction.FollowupAsync("팀 추첨 설정에 실패했습니다.", ephemeral: true);
        }
    }

    private static (int[] teams, int totalMembers) ParseTeamStr(string teamStr)
    {
        var tokens = teamStr.ToLowerInvariant().Split('v');
        var teams = tokens.Select(int.Parse).ToArray();
        return (teams, teams.Sum());
    }
}