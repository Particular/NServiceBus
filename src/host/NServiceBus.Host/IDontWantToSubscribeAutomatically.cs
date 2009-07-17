namespace NServiceBus.Host
{
    /// <summary>
    /// Indicate to the host that you don't want the bus to subscribe automatically to messages
    /// owned by other endpoints that are handled by this endpoint.
    /// 
    /// Implement this interface on the class which implements <see cref="IConfigureThisEndpoint"/>.
    /// </summary>
    public interface IDontWantToSubscribeAutomatically
    {
    }
}
