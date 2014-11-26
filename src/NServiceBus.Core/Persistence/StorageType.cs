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
        public class Timeouts : StorageType { }

        /// <summary>
        /// Storage for subscriptions
        /// </summary>
        public class Subscriptions : StorageType { }

        /// <summary>
        /// Storage for sagas
        /// </summary>
        public class Sagas : StorageType { }

        /// <summary>
        /// Storage for gateway de-duplication
        /// </summary>
        public class GatewayDeduplication : StorageType { }
        
        /// <summary>
        /// Storage for outbox
        /// </summary>
        public class Outbox : StorageType { }
    }
}