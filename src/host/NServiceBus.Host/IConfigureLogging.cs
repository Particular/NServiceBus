namespace NServiceBus.Host
{
    /// <summary>
    /// Called in order to configure logging.
    /// If <see cref="IDontWantProfiles"/> is set, then only one class should implement this directly.
    /// </summary>
    public interface IConfigureLogging
    {
        /// <summary>
        /// Performs all logging configuration.
        /// </summary>
        void ConfigureLogging();
    }

    /// <summary>
    /// Called in order to configure logging for the given profile type.
    /// If an implementation isn't found for a given profile, then the search continues
    /// recursively up that profile's inheritance hierarchy.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConfigureLoggingForProfile<T> : IConfigureLogging where T : IProfile {}
}
