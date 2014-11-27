namespace NServiceBus.Persistence
{
    /// <summary>
    /// The storage types used for NServiceBus needs
    /// </summary>
    public abstract class StorageType
    {
        /// <summary>
        /// Storage for timeouts
        /// </summary>
        public sealed class Timeouts : StorageType { }

        /// <summary>
        /// Storage for subscriptions
        /// </summary>
        public sealed class Subscriptions : StorageType { }

        /// <summary>
        /// Storage for sagas
        /// </summary>
        public sealed class Sagas : StorageType { }

        /// <summary>
        /// Storage for gateway de-duplication
        /// </summary>
        public sealed class GatewayDeduplication : StorageType { }
        
        /// <summary>
        /// Storage for outbox
        /// </summary>
        public sealed class Outbox : StorageType { }
    }
}