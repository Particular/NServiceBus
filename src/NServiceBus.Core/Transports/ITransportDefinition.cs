namespace NServiceBus.Transports
{
    /// <summary>
    /// Defines a transport that can be used by NServiceBus
    /// </summary>
    public interface ITransportDefinition
    {
    }

    /// <summary>
    /// Defines a transport that has native support for pub sub
    /// </summary>
    public interface HasNativePubSubSupport
    {
    }
}