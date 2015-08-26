namespace NServiceBus.Features
{
    /// <summary>
    /// Base class for all serialization <see cref="Feature"/>s.
    /// </summary>
    [ObsoleteEx(Message = "Use the ConfigureSerialization Feature class instead", TreatAsErrorFromVersion = "6.0", RemoveInVersion = "7.0", ReplacementTypeOrMember = "ConfigureSerialization")]
    public static class SerializationFeatureHelper
    {
    }
}