namespace NursingBot.Features
{
    public class NoRegisteredFeatureException : Exception
    {
        public static NoRegisteredFeatureException Instance => new();
    }
}