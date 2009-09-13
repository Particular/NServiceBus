namespace NServiceBus.Host
{
    /// <summary>
    /// Called in order to configure the bus.
    /// Implementors should work against the generic version of this interface.
    /// </summary>
    public interface IConfigureTheBus
    {
        /// <summary>
        /// Performs all bus configuration.
        /// </summary>
        void Configure(IConfigureThisEndpoint specifier);
    }

    /// <summary>
    /// Called in order to configure the bus for the given profile type.
    /// If an implementation isn't found for a given profile, then the search continues
    /// recursively up that profile's inheritance hierarchy.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConfigureTheBusForProfile<T> : IConfigureTheBus where T : IProfile {}
}
