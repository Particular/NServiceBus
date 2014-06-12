namespace NServiceBus.Persistence
{
    /// <summary>
    /// Base class for persistence definitions
    /// </summary>
    public abstract class PersistenceDefinition
    {
        /// <summary>
        /// Indicates if the persistence has support for storing outbox messages
        /// </summary>
        public bool HasOutboxStorage { get; protected set; }
        
        /// <summary>
        /// Indicates if the persistence has support for storing sagas
        /// </summary>
        public bool HasSagaStorage { get; protected set; }
        
        /// <summary>
        /// Indicates if the persistence has support for storing timeous
        /// </summary>
        public bool HasTimeoutStorage { get; protected set; }

        /// <summary>
        /// Indicates if the persistence has support for storing subscriptions
        /// </summary>
        public bool HasSubscriptionStorage { get; protected set; }

        /// <summary>
        /// Indicates if the persistence has support for storing gateway deduplication data
        /// </summary>
        public bool HasGatewaysStorage { get; protected set; }
    }
}