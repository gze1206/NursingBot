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

                #if DEBUG
                    DotNetEnv.Env.TraversePath().Load();
                #else
                    DotNetEnv.Env.Load();
                #endif
                
                var bot = new Bot();
                await bot.Initialize(DotNetEnv.Env.GetString("BOT_TOKEN"));
                
                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL ERROR ON PROGRAM : {ex.Message}\n\t{ex.StackTrace}");
            }
        }
    }
}