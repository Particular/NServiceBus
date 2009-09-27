namespace NServiceBus.Host
{
    /// <summary>
    /// If you want to specify your own container or serializer,
    /// implement this interface on the class which implements <see cref="IConfigureThisEndpoint"/>.
    /// 
    /// Implementors will be invoked before the endpoint starts up.
    /// Dependency injection is not provided for these types.
    /// </summary>
    public interface IWantCustomInitialization
    {
        /// <summary>
        /// Perform initialization logic.
        /// </summary>
        void Init();
    }

    /// <summary>
    /// If you want to specify your own logging,
    /// implement this interface on the class which implements <see cref="IConfigureThisEndpoint"/>. 
    /// </summary>
    public interface IWantCustomLogging
    {
        /// <summary>
        /// Initialize logging.
        /// </summary>
        void Init();
    }

    /// <summary>
    /// Implementers will be invoked when the endpoint starts up.
    /// Dependency injection is provided for these types.
    /// </summary>
    public interface IWantToRunAtStartup
    {
        /// <summary>
        /// Method called at startup.
        /// </summary>
        void Run();

        /// <summary>
        /// Method called on shutdown.
        /// </summary>
        void Stop();
    }

    /// <summary>
    /// Implementors will be provided with a reference to IConfigureThisEndpoint.
    /// Implementors must inherit either <see cref="IHandleProfile"/> or <see cref="IWantCustomInitialization"/>.
    /// </summary>
    public interface IWantTheEndpointConfig
    {
        /// <summary>
        /// This property will be set by the infrastructure.
        /// </summary>
        IConfigureThisEndpoint Config { get; set; }
    }
}
