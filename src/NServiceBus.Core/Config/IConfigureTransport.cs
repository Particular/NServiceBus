namespace NServiceBus.Config
{
    using Unicast.Transport;


    /// <summary>
    /// Configures the given transport using the default settings
    /// </summary>
    public interface IConfigureTransport
    {
        void Configure(Configure config);
    }


    /// <summary>
    /// The generic counterpart to IConfigureTransports
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConfigureTransport<T> : IConfigureTransport where T : ITransportDefinition{}
}