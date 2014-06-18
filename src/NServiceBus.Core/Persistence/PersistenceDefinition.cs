namespace NServiceBus.Persistence
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Base class for persistence definitions
    /// </summary>
    public abstract class PersistenceDefinition
    {
        /// <summary>
        /// Used be the storage definitions to declare what they suppoprt
        /// </summary>
        /// <param name="supported"></param>
        protected void Supports(params Storage[] supported)
        {
            supportedStorages.AddRange(supported.ToList());
        }

        /// <summary>
        /// True if supplied storage is supported
        /// </summary>
        /// <param name="storage"></param>
        /// <returns></returns>
        public bool HasSupportFor(Storage storage)
        {
            return SupportedStorages.Contains(storage);
        }

        internal IEnumerable<Storage> SupportedStorages { get { return supportedStorages.Distinct(); } }
 
        List<Storage> supportedStorages = new List<Storage>();
    }

    /// <summary>
    /// The storage needs of NServiceBus
    /// </summary>
    public enum Storage
    {
        Timeouts = 1,
        Subscriptions = 2,
        Sagas = 3,
        GatewayDeduplication = 4,
        Outbox = 5,
    }
}