#pragma warning disable 1591
namespace NServiceBus
{
    using System;

    public partial class TransportMessage
    {

        [ObsoleteEx(
            Message = "For sending purposes use DeliveryOptions.NonDurable (note the negation). When receiving look at the new 'NServiceBus.NonDurableMessage' header", 
            RemoveInVersion = "7.0", 
            TreatAsErrorFromVersion = "6.0")]
        public bool Recoverable { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        [ObsoleteEx(
            Message = "For sending purposes use DeliveryOptions.TimeToBeReceived. When receiving look at the new 'NServiceBus.TimeToBeReceived' header",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public TimeSpan TimeToBeReceived { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
    }
}