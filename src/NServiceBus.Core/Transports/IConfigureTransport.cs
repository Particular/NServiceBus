namespace NServiceBus.Transports
{
    /// <summary>
    /// Configures the given transport using the default settings
    /// </summary>
    public interface IConfigureTransport
    {
        /// <summary>
        /// Performs the configuration if selected
        /// </summary>
        /// <param name="config"></param>
        void Configure(Configure config);
    }


    /// <summary>
    /// The generic counterpart to IConfigureTransports
    /// </summary>
    public interface IConfigureTransport<T> : IConfigureTransport where T : TransportDefinition { }
}