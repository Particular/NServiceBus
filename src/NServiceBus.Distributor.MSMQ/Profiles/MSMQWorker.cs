namespace NServiceBus
{
    /// <summary>
    ///     Indicates that this node is a worker node that will process messages coming from its distributor.
    /// </summary>
    public interface MSMQWorker : IProfile
    {
    }
}