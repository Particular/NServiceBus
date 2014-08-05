namespace NServiceBus
{
    /// <summary>
    /// Called in order to configure logging for the given profile type.
    /// If an implementation isn't found for a given profile, then the search continues
    /// recursively up that profile's inheritance hierarchy.
    /// </summary>
    public interface IConfigureLoggingForProfile<T> : IConfigureLogging where T : IProfile {}
}