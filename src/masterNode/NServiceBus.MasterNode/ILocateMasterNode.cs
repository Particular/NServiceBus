namespace NServiceBus.MasterNode
{
    /// <summary>
    /// Provides ability to find out which machine is the "master node" for this endpoint.
    /// </summary>
    public interface ILocateMasterNode
    {
        /// <summary>
        /// Returns the name of the machine on which the master node is running.
        /// Implementors should cache the value as this method will be called multiple times.
        /// </summary>
        /// <returns></returns>
        string GetMasterNode();
    }
}
