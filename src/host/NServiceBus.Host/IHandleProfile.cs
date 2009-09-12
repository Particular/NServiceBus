namespace NServiceBus.Host
{
    /// <summary>
    /// Generic abstraction for code which will be called when the given profile is active.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IHandleProfile<T> : IHandleProfile where T : IProfile {}

    /// <summary>
    /// Generic abstraction for code which will configure how a given profile should behave.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IHandleProfileConfiguration<T> : IHandleProfileConfiguration where T : IProfile { }

    /// <summary>
    /// Abstraction for code which will be called when the given profile is active.
    /// Implementors should implement IHandleProfile{T} rather than IHandleProfile.
    /// </summary>
    public interface IHandleProfile
    {
        /// <summary>
        /// Called when a given profile is activated.
        /// </summary>
        void ProfileActivated();
    }

    /// <summary>
    /// Abstraction for code which will configure how a given profile should behave.
    /// </summary>
    public interface IHandleProfileConfiguration
    {
        /// <summary>
        /// Provides a reference for the endpoint configuration object.
        /// </summary>
        /// <param name="specifier"></param>
        void Init(IConfigureThisEndpoint specifier);

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
