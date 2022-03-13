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
                await bot
                    // Yes or No
                    .AddFeature(new Features.Random.YesOrNo())
                    .AddMigration(new Features.Random.YesOrNoRegister())
                    
                    // Magic-8-Ball
                    .AddFeature(new Features.Random.Magic8Ball())
                    .AddMigration(new Features.Random.Magic8BallRegister())

                    // Pick
                    .AddFeature(new Features.Random.Pick())
                    .AddMigration(new Features.Random.PickRegister())

                    // Initialize bot
                    .Initialize(DotNetEnv.Env.GetString("BOT_TOKEN"));
                
                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL ERROR ON PROGRAM : {ex.Message}\n\t{ex.StackTrace}");
            }
        }
    }
}