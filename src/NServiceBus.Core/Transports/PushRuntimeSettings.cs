namespace NServiceBus.Transports
{
    /// <summary>
    /// Contains the limits for how messages should be dequeued.
    /// </summary>
    public class PushRuntimeSettings
    {
        /// <summary>
        /// Restricts the concurrency.
        /// </summary>
        /// <param name="maxConcurrency">The max value to enforce.</param>
        public PushRuntimeSettings(int? maxConcurrency)
        {
            MaxConcurrency = maxConcurrency;
        }
        /// <summary>
        /// The maximum number of messages that should be in flight at any given time.
        /// </summary>
        public int? MaxConcurrency { get; private set; }


        /// <summary>
        /// Use default settings.
        /// </summary>
        public static PushRuntimeSettings Default
        {
            get { return new PushRuntimeSettings(null); }
        }
    }
}