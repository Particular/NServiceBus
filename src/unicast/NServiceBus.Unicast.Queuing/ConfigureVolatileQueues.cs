namespace NServiceBus
{
    public static class ConfigureVolatileQueues
    {
        /// <summary>
        /// Requests that queues will be created as Volatile (non-transactional) hence sending & receiving will be outside a transaction
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure VolatileQueues(this Configure config)
        {
            IsVolatileQueues = true;
            
            return config;
        }

        /// <summary>
        /// True queues should be created as Transactional (hence also sending will be within a transaction).
        /// </summary>
        public static bool IsVolatileQueues { get; private set; }
    }
}
