using System;

namespace NServiceBus
{
    /// <summary>
    /// Class for holding extension methods to NServiceBus.Configure
    /// </summary>
    public static class SyncConfig
    {
        /// <summary>
        /// Indicates whether the synchronization has been requested.
        /// </summary>
        public static bool Synchronize { get { return synchronize; } }

        /// <summary>
        /// Notify that configuration of ConfigureCommon occurred.
        /// </summary>
        public static void MarkConfigured() { configured = true; }

        /// <summary>
        /// Use this for multi-threaded rich clients. Specifies that message processing
        /// will occur within a synchronization domain (make sure that you only have one).
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure Synchronization(this Configure config)
        {
            if (configured)
                throw new InvalidOperationException("Synchronization() can only be called before any object builders.");

            synchronize = true;

            return config;
        }

        private static bool synchronize;
        private static bool configured;
    }
}
