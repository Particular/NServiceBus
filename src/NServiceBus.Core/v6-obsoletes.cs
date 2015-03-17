#pragma warning disable 1591
namespace NServiceBus
{
    using System;

    [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", ReplacementTypeOrMember = "config.ExcludeAssemblies")]
    public class AllAssemblies : IExcludesBuilder, IIncludesBuilder
    {
    }

    [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7")]
    public interface IExcludesBuilder
    {
    }

    [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7")]
    public interface IIncludesBuilder
    {
    }

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