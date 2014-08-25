#pragma warning disable 1591
namespace NServiceBus.Unicast.Subscriptions
{
    using System;

   [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
    public class SubscriptionEventArgs : EventArgs
    {
        public Address SubscriberReturnAddress { get; set; }

        public string MessageType { get; set; }
    }
}
