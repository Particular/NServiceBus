
namespace NServiceBus.Proxy
{
    using Unicast.Queuing;


    /// <summary>
    /// Signals to create proxy queue
    /// </summary>
    class ProxyQueueCreator : IWantQueueCreated
    {
        public MsmqProxyDataStorage MsmqProxyDataStorage { get; set; }

        /// <summary>
        /// Proxy queue address
        /// </summary>
        public Address Address
        {
            get { return MsmqProxyDataStorage.StorageQueue; }
        }

        /// <summary>
        /// Disable the creation of the proxy queue
        /// </summary>
        public bool IsDisabled
        {
            get { return (MsmqProxyDataStorage == null) || (MsmqProxyDataStorage.StorageQueue == null); }
        }
    }
}
