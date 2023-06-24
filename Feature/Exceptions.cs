namespace NursingBot.Feature;

public class NoRegisteredFeatureException : Exception
{
    public static NoRegisteredFeatureException Instance => new();
}