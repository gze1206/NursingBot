using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;

namespace NursingBot.Core
{
    // IFeature
    // 커맨드를 추상적으로 다루기 위한 인터페이스
    public interface IFeature
    {
        // TODO : IFeature 공통 기능 명세 작성하기
        // https://discord-nursing-bot.notion.site/IFeature-84eb35915a334aec88a4a373d1576b8c
        string Name { get; }
        Task ProcessCommand(SocketSlashCommand command);
    }

    // IFeatureMigration
    // 커맨드를 등록하거나 제거하는 작업을 수행하기 위해 사용
    public interface IFeatureMigration
    {
        Task Migrate(DiscordSocketClient client, ConcurrentBag<ApplicationCommandProperties> commandBag);
    }
}