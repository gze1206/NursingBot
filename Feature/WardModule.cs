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

namespace NursingBot.Feature
{
    [RequireRegister]
    [Group("ward")]
    public class WardModule : ModuleBase<SocketCommandContext>
    {
        protected override void OnModuleBuilding(CommandService commandService, ModuleBuilder builder)
        {
            base.OnModuleBuilding(commandService, builder);

            Global.Bot!.Client.UserVoiceStateUpdated += (user, oldVoiceState, newVoiceState) =>
            {
                _ = OnUserVoiceState(user, oldVoiceState, newVoiceState);
                return Task.CompletedTask;
            };
        }

        [RequireAdminPermission]
        [Command("category")]
        public async Task SetCategoryAsync(SocketCategoryChannel category)
        {
            if (category == null)
            {
                return;
            }

            if (!Database.CachedServers.TryGetValue(category.Guild.Id, out var server))
            {
                await this.Context.Message.ReplyAsync("서버 정보 조회에 실패했습니다...");
                return;
            }

            using var conn = await Database.Instance.CreateDbContextAsync();
            using var transaction = await conn.Database.BeginTransactionAsync();

            try
            {
                var hasDuplicated = await conn.WardConfigs.AnyAsync(wardConfig =>
                    wardConfig.ServerId == server.Id
                    && !wardConfig.IsDeleted);

                if (hasDuplicated)
                {
                    await this.Context.Message.ReplyAsync("이미 카테고리가 설정되어 있습니다.");
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
                await this.Context.Message.ReplyAsync("카테고리 설정이 완료되었습니다.");
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                await Log.Fatal(e);
            }
        }

        [RequireAdminPermission]
        [Command("disable")]
        public async Task DisableAsync()
        {
            if (!Database.CachedServers.TryGetValue(this.Context.Guild.Id, out var server))
            {
                await this.Context.Message.ReplyAsync("서버 정보 조회에 실패했습니다...");
                return;
            }

            using var conn = await Database.Instance.CreateDbContextAsync();
            using var transaction = await conn.Database.BeginTransactionAsync();

            try
            {
                var wardConfig = await conn.WardConfigs.SingleOrDefaultAsync(wardConfig =>
                    wardConfig.ServerId == server.Id
                    && !wardConfig.IsDeleted);

                if (wardConfig == null)
                {
                    await this.Context.Message.ReplyAsync("카테고리가 설정되어 있지 않습니다.");
                    await transaction.RollbackAsync();
                    return;
                }

                wardConfig.IsDeleted = true;
                wardConfig.DeletedAt = DateTime.UtcNow;
                wardConfig.UpdatedAt = DateTime.UtcNow;
                conn.WardConfigs.Update(wardConfig);
                await conn.SaveChangesAsync();

                await transaction.CommitAsync();
                await this.Context.Message.ReplyAsync("입원 기능이 비활성화되었습니다.");
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                await Log.Fatal(e);
            }
        }

        [Command("rename")]
        public async Task RenameAsync([Remainder] string newName)
        {
            if (!Database.CachedServers.TryGetValue(this.Context.Guild.Id, out var server))
            {
                await this.Context.Message.ReplyAsync("서버 정보 조회에 실패했습니다...");
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
                await this.Context.Message.ReplyAsync("현재 음성 채널에 입장해있지 않습니다.");
                return;
            }

            if (channel.CategoryId == null)
            {
                return;
            }

            using var conn = await Database.Instance.CreateDbContextAsync();
            using var transaction = await conn.Database.BeginTransactionAsync();

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
                await this.Context.Message.ReplyAsync($"{this.Context.User.Mention} 병실 이름 변경에 성공했습니다!\n{oldName} -> {newName}");
                await Log.Info($"{this.Context.User.Username} 병실 이름 변경 / {oldName} -> {newName}");
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                await Log.Fatal(e);
                await this.Context.Message.ReplyAsync("음성 채널 이름 변경에 실패했습니다...");
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

            using var conn = await Database.Instance.CreateDbContextAsync();
            using var transaction = await conn.Database.BeginTransactionAsync();

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

                    if (wardConfig != null)
                    {
                        var ward = await newChannel.Guild.CreateVoiceChannelAsync($"{user.DisplayName}의 병실",
                            properties => properties.CategoryId = wardConfig.CategoryId);

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
}
