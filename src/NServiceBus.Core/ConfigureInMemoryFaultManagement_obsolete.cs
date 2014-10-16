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
        [ObsoleteEx(
            Message = "Use `configuration.DiscardFailedMessagesInsteadOfSendingToErrorQueue()`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces.", 
            RemoveInVersion = "6.0", 
            TreatAsErrorFromVersion = "5.0")]
        // ReSharper disable UnusedParameter.Global
        public static Configure InMemoryFaultManagement(this Configure config)
        {
            throw new InvalidOperationException();
        }
    }
}