#pragma warning disable 1591
namespace NServiceBus
{
    using System;

    [ObsoleteEx(RemoveInVersion = "7",TreatAsErrorFromVersion = "6")]
    public static class ConfigureInMemoryFaultManagement
    {
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
