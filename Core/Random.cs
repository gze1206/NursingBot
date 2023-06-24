using SRandom = System.Random;

namespace NursingBot.Core;

public static class Random
{
    private static readonly SRandom global = new();
        
    [ThreadStatic]
    private static SRandom? local = null;

    private static void CheckInstance()
    {
        if (local == null)
        {
            int seed;
            lock (global)
            {
                seed = global.Next();
            }
            local = new(seed);
        }
    }

    public static int Next()
    {
        CheckInstance();

        return local!.Next();
    }

    public static int Next(int max)
    {
        CheckInstance();

        return local!.Next(max);
    }

    public static int Next(int min, int max)
    {
        CheckInstance();

        return local!.Next(min, max);
    }
}