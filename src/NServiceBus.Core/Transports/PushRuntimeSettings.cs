namespace NServiceBus.Transport
{
    using System;

    /// <summary>
    /// Controls how the message pump should behave.
    /// </summary>
    public class PushRuntimeSettings
    {
        /// <summary>
        /// Constructs the settings.
        /// </summary>
        /// <param name="maxConcurrency">The maximum concurrency to allow. Zero allows NServiceBus to pick a suitable default.</param>
        public PushRuntimeSettings(int maxConcurrency = 0)
        {
            Guard.AgainstNegative(nameof(maxConcurrency), maxConcurrency);

            if (maxConcurrency == 0)
            {
                maxConcurrency = Math.Max(2, Environment.ProcessorCount);
            }

            MaxConcurrency = maxConcurrency;
        }

        /// <summary>
        /// The maximum number of messages that should be in flight at any given time.
        /// </summary>
        public int MaxConcurrency { get; private set; }


        /// <summary>
        /// Use default settings.
        /// </summary>
        public static PushRuntimeSettings Default => new PushRuntimeSettings();
    }
}