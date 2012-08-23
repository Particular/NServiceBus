namespace NServiceBus.Config
{
    /// <summary>
    /// Configuration class for Endpoint attributes.
    /// </summary>
    public static class Endpoint
    {
        /// <summary>
        /// Requests that queues will be created as Volatile (non-transactional) hence sending and receiving will be outside a transaction
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure Volatile(this Configure config)
        {
            IsVolatile = true;
            DontUseDistributedTransactions = true;
            return config;
        }

        /// <summary>
        /// True queues should be created as Transactional (hence also sending will be within a transaction).
        /// </summary>
        public static bool IsVolatile { get; private set; }

        /// <summary>
        /// Initialized the bus in send only mode
        /// </summary>
        /// <returns></returns>
        public static IBus SendOnly(this Configure config)
        {
            IsSendOnly = true;
            config.Initialize();
            return config.Builder.Build<IBus>();
        }

        /// <summary>
        /// True if this endpoint is operating in send only mode
        /// </summary>
        public static bool IsSendOnly { get; private set; }

        /// <summary>
        /// True if distributed transactions should not be used
        /// </summary>
        public static bool DontUseDistributedTransactions{ get; set; }
    }
}
