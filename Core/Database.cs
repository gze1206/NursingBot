using System.Data;
using NursingBot.Logger;
using NursingBot.Models;
using SQLite;

namespace NursingBot.Core
{
    public static class Database
    {
        #pragma warning disable
        public static SQLiteAsyncConnection Instance { get; private set; }
        #pragma warning enable

        public static Dictionary<ulong, Server> CachedServers { get; private set; } = new();

        public static async Task Initialize(string path)
        {
            Instance = new SQLiteAsyncConnection(path);

            // 테이블 생성
            await Instance.CreateTableAsync<Server>();
            await Instance.CreateTableAsync<PartyChannel>();
            await Instance.CreateTableAsync<PartyRecruit>();
        }

        public static void Cache(ulong guildId, Server server)
        {
            CachedServers[guildId] = server;
        }

        public static void ClearCache(ulong guildId)
        {
            CachedServers.Remove(guildId);
        }
    }
}