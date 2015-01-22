namespace NServiceBus
{
    using System;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure
    /// </summary>
    [ObsoleteEx(RemoveInVersion = "7",TreatAsErrorFromVersion = "6")]
    public static partial class ConfigureInMemoryFaultManagement
    {
        /// <summary>
        /// Tells the endpoint to discard messages that fails
        /// </summary>
        [ObsoleteEx(
      Message = "This is no longer supported. If you want full control over what happens when a message fails (including retries) please override the MoveFaultsToErrorQueue behavior.",
      RemoveInVersion = "7",
      TreatAsErrorFromVersion = "6")]
        public static void DiscardFailedMessagesInsteadOfSendingToErrorQueue(this BusConfiguration config)
        {
            throw new NotImplementedException();
        }
    }
}
