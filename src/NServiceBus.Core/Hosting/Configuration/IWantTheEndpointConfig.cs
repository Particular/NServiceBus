namespace NServiceBus
{
    using Hosting.Profiles;

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