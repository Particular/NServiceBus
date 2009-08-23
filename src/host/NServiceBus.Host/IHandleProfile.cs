namespace NServiceBus.Host
{
    /// <summary>
    /// Generic abstraction for code which handles configuration of a given profile.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IHandleProfile<T> : IHandleProfile where T : IProfile {}

    /// <summary>
    /// Abstraction for code which handles configuration of a given profile
    /// </summary>
    public interface IHandleProfile
    {
        /// <summary>
        /// This method called before all others - you may want to store the given specifier.
        /// </summary>
        /// <param name="specifier"></param>
        void Init(IConfigureThisEndpoint specifier);

        /// <summary>
        /// Configure how log4net is set up.
        /// </summary>
        void ConfigureLogging();

        /// <summary>
        /// Configure how sagas are handled.
        /// </summary>
        /// <param name="busConfiguration"></param>
        void ConfigureSagas(Configure busConfiguration);

        /// <summary>
        /// Configure which storage should be used for subscription information.
        /// </summary>
        /// <param name="busConfiguration"></param>
        void ConfigureSubscriptionStorage(Configure busConfiguration);
    }
}
