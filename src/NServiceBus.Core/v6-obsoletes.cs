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



        [ObsoleteEx(
          Message = "Not used anymore, you most likely should use a `OutgoingMessageInstead`",
          RemoveInVersion = "7.0",
          TreatAsErrorFromVersion = "6.0")]

        public TransportMessage()
        {
            throw new NotImplementedException();
        }

    }
}


namespace NServiceBus.Unicast
{
    using System;

    public abstract partial class DeliveryOptions
    {
        [ObsoleteEx(
           Message = "Reply to address can be get/set using the `NServiceBus.ReplyToAddress` header",
           RemoveInVersion = "7.0",
           TreatAsErrorFromVersion = "6.0")]
        public string ReplyToAddress { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
    }
}


namespace NServiceBus.Unicast
{
    using System;

    public partial class SendOptions
    {

        [ObsoleteEx(
    Message = "Reply to address can be get/set using the `NServiceBus.CorrelationId` header",
    RemoveInVersion = "7.0",
    TreatAsErrorFromVersion = "6.0")]
        public string CorrelationId { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

    }
}

namespace NServiceBus.Timeout.Core
{
    using System;
    using NServiceBus.Unicast;

    public partial class TimeoutData
    {


        [ObsoleteEx(Message = "Use new OutgoingMessage(timeoutData.State) instead", RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0")]
        public TransportMessage ToTransportMessage()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use new SendOptions() instead",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public SendOptions ToSendOptions(Address replyToAddress)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use new SendOptions() instead",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public SendOptions ToSendOptions(string replyToAddress)
        {
            throw new NotImplementedException();
        }



        [ObsoleteEx(
            Message = "Not used anymore",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public const string OriginalReplyToAddress = "NServiceBus.Timeout.ReplyToAddress";
    }
}


namespace NServiceBus.Unicast
{
    using System;


    [ObsoleteEx(
            Message = "Not used anymore, use the 'NServiceBus.MessageIntent' header to detect if the message is a reply",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
    public class ReplyOptions : SendOptions
    {


        public ReplyOptions(string destination)
            : base(destination)
        {
            throw new NotImplementedException();
        }


        public ReplyOptions(Address destination, string correlationId)
            : base(destination)
        {
            throw new NotImplementedException();
        }


        public ReplyOptions(string destination, string correlationId)
            : base(destination)
        {
            throw new NotImplementedException();
        }
    }
}