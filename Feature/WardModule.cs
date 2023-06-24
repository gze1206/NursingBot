using Discord;
using Discord.Interactions;
using Discord.Interactions.Builders;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NursingBot.Core;
using NursingBot.Feature.Preconditions;
using NursingBot.Logger;
using NursingBot.Models;

namespace NursingBot.Feature;

[RequireRegister]
[Group("ward", "병실(자동으로 만들고 지워지는 음성채널)에 관련된 기능입니다.")]
public class WardModule : InteractionModuleBase<SocketInteractionContext>
{
    public override void OnModuleBuilding(InteractionService interactionService, ModuleInfo moduleInfo)
    {
        base.OnModuleBuilding(interactionService, moduleInfo);

        Global.Bot!.Client.UserVoiceStateUpdated += (user, oldVoiceState, newVoiceState) =>
        {
            _ = OnUserVoiceState(user, oldVoiceState, newVoiceState);
            return Task.CompletedTask;
        };
    }

    [RequireAdminPermission]
    [SlashCommand("category", "입원 기능을 활성화할 카테고리를 지정합니다.\n해당 카테고리의 모든 채널이 삭제되고, 입원 신청 채널이 생성됩니다.\n해당 채널에 유저가 진입하면 새 채널을 만듭니다.")]
    public async Task SetCategoryAsync([Summary("category", "입원 기능에 사용할 카테고리를 고릅니다.")]SocketCategoryChannel? category)
    {
        if (category == null)
        {
            return;
        }

        if (!Database.CachedServers.TryGetValue(category.Guild.Id, out var server))
        {
            await this.Context.Interaction.RespondAsync("서버 정보 조회에 실패했습니다...", ephemeral: true);
            return;
        }

        await using var conn = await Database.Instance.CreateDbContextAsync();
        await using var transaction = await conn.Database.BeginTransactionAsync();

        try
        {
            var hasDuplicated = await conn.WardConfigs.AnyAsync(wardConfig =>
                wardConfig.ServerId == server.Id
                && !wardConfig.IsDeleted);

            if (hasDuplicated)
            {
                await this.Context.Interaction.RespondAsync("이미 카테고리가 설정되어 있습니다.", ephemeral: true);
                await transaction.RollbackAsync();
                return;
            }

            await Task.WhenAll(category.Channels.Select(channel => channel.DeleteAsync()));
            var hospitalization = await category.Guild.CreateVoiceChannelAsync("입원 신청", properties => properties.CategoryId = category.Id);

            var wardConfig = new WardConfig
            {
                ServerId = server.Id,
                CategoryId = category.Id,
                HospitalizationId = hospitalization.Id,
            };

            await conn.WardConfigs.AddAsync(wardConfig);
            await conn.SaveChangesAsync();

            await transaction.CommitAsync();
            await this.Context.Interaction.RespondAsync("카테고리 설정이 완료되었습니다.");
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            await Log.Fatal(e);
        }
    }

    [RequireAdminPermission]
    [SlashCommand("disable", "이 서버에서 입원 기능을 비활성화합니다.\n더 이상 입원 채널에 유저가 진입해도 새 병실을 만들지 않습니다.\n카테고리를 변경하기 위해선 이 명령어로 이전 설정을 지워주어야 합니다.")]
    public async Task DisableAsync()
    {
        if (!Database.CachedServers.TryGetValue(this.Context.Guild.Id, out var server))
        {
            await this.Context.Interaction.RespondAsync("서버 정보 조회에 실패했습니다...", ephemeral: true);
            return;
        }

        await using var conn = await Database.Instance.CreateDbContextAsync();
        await using var transaction = await conn.Database.BeginTransactionAsync();

        try
        {
            var wardConfig = await conn.WardConfigs.SingleOrDefaultAsync(wardConfig =>
                wardConfig.ServerId == server.Id
                && !wardConfig.IsDeleted);

            if (wardConfig == null)
            {
                await this.Context.Interaction.RespondAsync("카테고리가 설정되어 있지 않습니다.", ephemeral: true);
                await transaction.RollbackAsync();
                return;
            }

            wardConfig.IsDeleted = true;
            wardConfig.DeletedAt = DateTime.UtcNow;
            wardConfig.UpdatedAt = DateTime.UtcNow;
            conn.WardConfigs.Update(wardConfig);
            await conn.SaveChangesAsync();

            await transaction.CommitAsync();
            await this.Context.Interaction.RespondAsync("입원 기능이 비활성화되었습니다.");
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            await Log.Fatal(e);
        }
    }

    [SlashCommand("rename", "현재 입장해있는 병실의 이름을 바꿉니다.\n병실에 입장해있다면 누구나 이 명령어를 사용할 수 있습니다.")]
    public async Task RenameAsync([Summary("new_name", "사용할 새 이름")]string newName)
    {
        if (!Database.CachedServers.TryGetValue(this.Context.Guild.Id, out var server))
        {
            await this.Context.Interaction.RespondAsync("서버 정보 조회에 실패했습니다...", ephemeral: true);
            return;
        }

        if (this.Context.User is not SocketGuildUser user)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(newName))
        {
            return;
        }

        var channel = user.VoiceChannel;

        if (channel == null)
        {
            await this.Context.Interaction.RespondAsync("현재 음성 채널에 입장해있지 않습니다.", ephemeral: true);
            return;
        }

        if (channel.CategoryId == null)
        {
            await this.Context.Interaction.RespondAsync("현재 병실에 입장해있지 않습니다.", ephemeral: true);
            return;
        }

        await using var conn = await Database.Instance.CreateDbContextAsync();
        await using var transaction = await conn.Database.BeginTransactionAsync();

        try
        {
            var wardConfig = await conn.WardConfigs
                .FirstOrDefaultAsync(wardConfig =>
                    wardConfig.ServerId == server.Id
                    && wardConfig.CategoryId == channel.CategoryId
                    && !wardConfig.IsDeleted);

            if (wardConfig == null)
            {
                await transaction.RollbackAsync();
                return;
            }

            if (channel.Id == wardConfig.HospitalizationId)
            {
                await transaction.RollbackAsync();
                return;
            }

            var oldName = channel.Name;
            await channel.ModifyAsync(properties => properties.Name = newName);
            await this.Context.Interaction.RespondAsync($"{this.Context.User.Mention} 병실 이름 변경에 성공했습니다!\n{oldName} -> {newName}");
            await Log.Info($"{this.Context.User.Username} 병실 이름 변경 / {oldName} -> {newName}");
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            await Log.Fatal(e);
            await this.Context.Interaction.RespondAsync("음성 채널 이름 변경에 실패했습니다...", ephemeral: true);
        }
    }

    [SlashCommand("limit", "현재 입장해있는 병실의 인원 제한을 바꿉니다.\n병실에 입장해있다면 누구나 이 명령어를 사용할 수 있습니다.")]
    public async Task LimitAsync([Summary("new_limits", "사용할 새 제한")]int value)
    {
        if (!Database.CachedServers.TryGetValue(this.Context.Guild.Id, out var server))
        {
            await this.Context.Interaction.RespondAsync("서버 정보 조회에 실패했습니다...", ephemeral: true);
            return;
        }

        if (this.Context.User is not SocketGuildUser user)
        {
            return;
        }

        if (value > 99)
        {
            await this.Context.Interaction.RespondAsync("최대 99까지 입력 가능합니다.", ephemeral: true);
            return;
        }

        var channel = user.VoiceChannel;

        if (channel == null)
        {
            await this.Context.Interaction.RespondAsync("현재 음성 채널에 입장해있지 않습니다.", ephemeral: true);
            return;
        }

        if (channel.CategoryId == null)
        {
            await this.Context.Interaction.RespondAsync("현재 병실에 입장해있지 않습니다.", ephemeral: true);
            return;
        }

        await using var conn = await Database.Instance.CreateDbContextAsync();
        await using var transaction = await conn.Database.BeginTransactionAsync();

        try
        {
            var wardConfig = await conn.WardConfigs
                .FirstOrDefaultAsync(wardConfig =>
                    wardConfig.ServerId == server.Id
                    && wardConfig.CategoryId == channel.CategoryId
                    && !wardConfig.IsDeleted);

            if (wardConfig == null)
            {
                await transaction.RollbackAsync();
                return;
            }

            if (channel.Id == wardConfig.HospitalizationId)
            {
                await transaction.RollbackAsync();
                return;
            }

            var name = channel.Name;
            var oldLimit = channel.UserLimit;
            int? newLimit = value < 1 ? null : value;
            await channel.ModifyAsync(properties => properties.UserLimit = newLimit);
            await this.Context.Interaction.RespondAsync($"{this.Context.User.Mention} 병실 인원 제한 변경에 성공했습니다!\n{LimitToString(oldLimit)} -> {LimitToString(newLimit)}");
            await Log.Info($"{this.Context.User.Username} 병실 인원 제한 변경 / {LimitToString(oldLimit)} -> {LimitToString(newLimit)}");

            static string LimitToString(int? value)
            {
                return value?.ToString() ?? "무제한";
            }
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            await Log.Fatal(e);
            await this.Context.Interaction.RespondAsync("음성 채널 이름 변경에 실패했습니다...", ephemeral: true);
        }
    }

    private static async Task OnUserVoiceState(SocketUser socketUser, SocketVoiceState oldVoiceState, SocketVoiceState newVoiceState)
    {
        if (socketUser is not SocketGuildUser user)
        {
            return;
        }

        var guild = user.Guild;
        var oldChannel = oldVoiceState.VoiceChannel;
        var newChannel = newVoiceState.VoiceChannel;

        // 같은 채널에서 이벤트만 들어온거면 처리 대상이 아님
        if (oldChannel?.Id == newChannel?.Id)
        {
            return;
        }

        await using var conn = await Database.Instance.CreateDbContextAsync();
        await using var transaction = await conn.Database.BeginTransactionAsync();

        try
        {
            var server = await conn.Servers.FirstOrDefaultAsync(s => s.DiscordUID == guild.Id);

            if (server == null)
            {
                await transaction.RollbackAsync();
                return;
            }

            // 입원 신청 체크
            // = 새로 들어간 음성 채널이 입원 신청 채널이다
            if (newChannel != null && newChannel.CategoryId != null)
            {
                var wardConfig = await conn.WardConfigs
                    .FirstOrDefaultAsync(wardConfig =>
                        wardConfig.ServerId == server.Id
                        && wardConfig.CategoryId == newChannel.CategoryId
                        && wardConfig.HospitalizationId == newChannel.Id
                        && !wardConfig.IsDeleted);

                if (wardConfig != null && newChannel.CategoryId == wardConfig.CategoryId)
                {
                    var ward = await newChannel.Guild.CreateVoiceChannelAsync($"{user.DisplayName}의 병실",
                        properties =>
                        {
                            // 입원 신청 채널에서 가져올 세팅들 : 카테고리, 비트레이트, 지역, 최대 인원, 권한 오버라이드
                            properties.CategoryId = newChannel.CategoryId;
                            properties.Bitrate = newChannel.Bitrate;
                            properties.RTCRegion = newChannel.RTCRegion;
                            properties.UserLimit = newChannel.UserLimit;
                            properties.PermissionOverwrites = newChannel.PermissionOverwrites.ToList();
                        });

                    await guild.MoveAsync(user, ward);
                    await Log.Info($"{user.DisplayName} 병실 생성");
                }
            }
                
            // 병실에 아무도 없으면 폐쇄
            // = 병동 카테고리인 음성 채널에서 나갔는데, 거기가 입원 신청 채널이 아니고, 이제 거기 아무도 없다
            if (oldChannel != null && oldChannel.CategoryId != null)
            {
                var wardConfig = await conn.WardConfigs
                    .FirstOrDefaultAsync(wardConfig =>
                        wardConfig.ServerId == server.Id
                        && wardConfig.CategoryId == oldChannel.CategoryId
                        && !wardConfig.IsDeleted);

                if (wardConfig == null)
                {
                    await transaction.RollbackAsync();
                    return;
                }

                // 입원 신청 채널에서 나간거면 처리 대상이 아님
                if (oldChannel.Id == wardConfig.HospitalizationId)
                {
                    await transaction.RollbackAsync();
                    return;
                }

                // 그 채널에 봇이 아닌 유저가 한 명이라도 남아있으면 처리 대상이 아님
                if (oldChannel.Users.Any(u => !u.IsBot))
                {
                    await transaction.RollbackAsync();
                    return;
                }

                // 그 병실을 폐쇄
                await oldChannel.DeleteAsync();
                await Log.Info($"{user.DisplayName} 나감 -> {oldChannel.Name} 병실 소멸");
            }
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            await Log.Fatal(e);
            return;
        }
    }
}