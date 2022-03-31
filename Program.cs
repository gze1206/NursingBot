using MySqlConnector;
using NursingBot.Core;
using NursingBot.Logger;

namespace NursingBot
{
    public class Program
    {
        public static Task? Main(string[] _) => MainAsync();

        public static async Task MainAsync()
        {
            try
            {
                await Log.Add(new ConsoleLogger());
                await Log.Add(new NLogLogger());

#if DEBUG
                DotNetEnv.Env.TraversePath().Load();
#else
                DotNetEnv.Env.Load();
#endif

                var conn = new MySqlConnectionStringBuilder()
                {
                    Server = DotNetEnv.Env.GetString("DB_SERVER"),
                    Database = DotNetEnv.Env.GetString("DB_NAME"),
                    UserID = DotNetEnv.Env.GetString("DB_USER"),
                    Password = DotNetEnv.Env.GetString("DB_PW"),
                };

                await Database.Initialize(conn.ConnectionString);

                var superUser = DotNetEnv.Env.GetString("SUPER_USER");
                if (!string.IsNullOrWhiteSpace(superUser)
                    && ulong.TryParse(superUser, out var superUserId))
                {
                    Global.SuperUserId = superUserId;
                }

                Global.Bot = new Bot();
                await Global.Bot.Initialize(DotNetEnv.Env.GetString("BOT_TOKEN"));
                
                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL ERROR ON PROGRAM : {ex.Message}\n\t{ex.StackTrace}");
                Console.ReadKey(true);
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
        }
    }
}