namespace NServiceBus
{
    /// <summary>
    /// Determines how the azure location behaves.
    /// </summary>
    public enum AddressMode
    {
        /// <summary>
        /// Addressing behavior is confirm to local queuing policies, eg. MSMQ.
        /// </summary>
        Local,

        /// <summary>
        /// Addressing behavior is confirm to remote queuing policies, eg. Azure.
        /// </summary>
        Remote
    }
}