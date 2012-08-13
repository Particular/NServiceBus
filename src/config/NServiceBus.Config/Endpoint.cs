namespace NServiceBus.Config
{
    /// <summary>
    /// COnfiguration class for Endpoint attributes.
    /// </summary>
    public static class Endpoint
    {
        /// <summary>
        /// Requests that queues will be created as Volatile (non-transactional) hence sending & receiving will be outside a transaction
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure Volatile(this Configure config)
        {
            IsVolatile = true;

            return config;
        }

        /// <summary>
        /// True queues should be created as Transactional (hence also sending will be within a transaction).
        /// </summary>
        public static bool IsVolatile { get; private set; }
    }
}
