namespace NServiceBus
{
    /// <summary>
    /// Determines how the azure location behaves
    /// </summary>
    public enum AddressMode
    {
        /// <summary>
        /// Addressing behavior is confirm to local queueing policies, eg. MSMQ
        /// </summary>
        Local,
        /// <summary>
        /// Addressing behavior is confirm to remote queueing policies, eg. Azure
        /// </summary>
        Remote
    }
}