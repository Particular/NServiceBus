namespace NServiceBus
{
    /// <summary>
    /// Indicates that this node is the master node for its set of messages.
    /// </summary>
    public interface Master : IProfile
    {
    }

    /// <summary>
    /// Indicates that this node will attempt to discover where other nodes
    /// are on the network.
    /// </summary>
    public interface DynamicDiscovery : IProfile
    {
    }
}
