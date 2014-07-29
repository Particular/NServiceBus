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
        /// Used be the storage definitions to declare what they support
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
        /// <summary>
        /// Storage for timeouts
        /// </summary>
        Timeouts = 1,
        /// <summary>
        /// Storage for subscriptions
        /// </summary>
        Subscriptions = 2,
        /// <summary>
        /// Storage for sagas
        /// </summary>
        Sagas = 3,
        /// <summary>
        /// Storage for gateway deduplication
        /// </summary>
        GatewayDeduplication = 4,
        /// <summary>
        /// Storage for the outbox
        /// </summary>
        Outbox = 5,
    }
}