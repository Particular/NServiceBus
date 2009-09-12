namespace NServiceBus.Host
{
    /// <summary>
    /// Indicate to the host that you don't want profiles.
    /// </summary>
    public interface IDontWantProfiles {}

    /// <summary>
    /// Indicate to the host that you don't want the bus to subscribe automatically to messages
    /// owned by other endpoints that are handled by this endpoint.
    /// </summary>
    public interface IDontWantToSubscribeAutomatically { }
}
