namespace NServiceBus.Transport
{
    using System;

    /// <summary>
    /// Controls how the message pump should behave.
    /// </summary>
    public class PushRuntimeSettings
    {
        /// <summary>
        /// Constructs the settings. NServiceBus will pick a suitable default for `MaxConcurrency`.
        /// </summary>
        public PushRuntimeSettings()
        {
            MaxConcurrency = Math.Max(2, Environment.ProcessorCount);
        }

        /// <summary>
        /// Constructs the settings.
        /// </summary>
        /// <param name="maxConcurrency">The maximum concurrency to allow.</param>
        public PushRuntimeSettings(int maxConcurrency)
        {
            Guard.AgainstNegativeAndZero(nameof(maxConcurrency), maxConcurrency);

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