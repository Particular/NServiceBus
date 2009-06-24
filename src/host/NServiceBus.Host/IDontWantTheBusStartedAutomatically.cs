namespace NServiceBus.Host
{
    /// <summary>
    /// Indicate to the host that you don't want the bus to be started automatically.
    /// Implement this interface on the class which implements <see cref="IConfigureThisEndpoint"/>.
    /// </summary>
    public interface IDontWantTheBusStartedAutomatically
    {
    }
}
