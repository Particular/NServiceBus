namespace NServiceBus
{
    using System;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure
    /// </summary>
    public static partial class ConfigureInMemoryFaultManagement
    {
        /// <summary>
        /// Use in-memory fault management.
        /// </summary>
        [ObsoleteEx(Replacement = "Use configuration.DiscardFailedMessagesInsteadOfSendingToErrorQueue(), where configuration is an instance of type BusConfiguration", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        // ReSharper disable UnusedParameter.Global
        public static Configure InMemoryFaultManagement(this Configure config)
        {
            throw new InvalidOperationException();
        }
    }
}