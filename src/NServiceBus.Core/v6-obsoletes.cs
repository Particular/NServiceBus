#pragma warning disable 1591
namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public partial class BusConfiguration
    {
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "ExcludeTypes")]
        public void TypesToScan(IEnumerable<Type> typesToScan)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "ExcludeAssemblies")]
        public void AssembliesToScan(IEnumerable<Assembly> assemblies)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "ExcludeAssemblies")]
        public void AssembliesToScan(params Assembly[] assemblies)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "ExcludeAssemblies")]
        public void ScanAssembliesInDirectory(string probeDirectory)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "OverridePublicReturnAddress(string address)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public void OverridePublicReturnAddress(Address address)
        {
            throw new NotImplementedException();
        }

    }

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
            Message = "For sending purposes use DeliveryMessageOptions.NonDurable (note the negation). When receiving look at the new 'NServiceBus.NonDurableMessage' header",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public bool Recoverable { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        [ObsoleteEx(
            Message = "For sending purposes use DeliveryMessageOptions.TimeToBeReceived. When receiving look at the new 'NServiceBus.TimeToBeReceived' header",
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

    [ObsoleteEx(
            ReplacementTypeOrMember = "NServiceBus.UnicastBus.DeliveryMessageOptions",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
    public abstract class DeliveryOptions
    {
        [ObsoleteEx(
           Message = "Reply to address can be get/set using the `NServiceBus.ReplyToAddress` header",
           RemoveInVersion = "7.0",
           TreatAsErrorFromVersion = "6.0")]
        public string ReplyToAddress { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
    }

    [ObsoleteEx(
            ReplacementTypeOrMember = "NServiceBus.UnicastBus.PublishMessageOptions",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
    public class PublishOptions : DeliveryOptions
    {
    }

    [ObsoleteEx(
            ReplacementTypeOrMember = "NServiceBus.UnicastBus.SendMessageOptions",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
    public class SendOptions : DeliveryOptions
    {
        [ObsoleteEx(
            ReplacementTypeOrMember = "SendMessageOptions(string)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        // ReSharper disable once UnusedParameter.Local
        public SendOptions(Address destination)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Reply to address can be get/set using the `NServiceBus.CorrelationId` header",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public string CorrelationId
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "DelayDeliveryFor",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public TimeSpan? DelayDeliveryWith
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
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
        public SendMessageOptions ToSendOptions(Address replyToAddress)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use new SendOptions() instead",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public SendMessageOptions ToSendOptions(string replyToAddress)
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
    [ObsoleteEx(
            Message = "Not used anymore, use the 'NServiceBus.MessageIntent' header to detect if the message is a reply",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
    public class ReplyOptions : DeliveryOptions
    {
    }
}

namespace NServiceBus.MessageMutator
{
    [ObsoleteEx(
              Message = "Just have your mutator implement both IMutateOutgoingMessages and IMutateIncomingMessages ",
              RemoveInVersion = "7.0",
              TreatAsErrorFromVersion = "6.0")]
    public interface IMessageMutator : IMutateOutgoingMessages, IMutateIncomingMessages { }
}

namespace NServiceBus.MessageMutator
{
    [ObsoleteEx(
                Message = "Just have your mutator implement both IMutateIncomingTransportMessages and IMutateOutgoingTransportMessages ",
                RemoveInVersion = "7.0",
                TreatAsErrorFromVersion = "6.0")]
    public interface IMutateTransportMessages : IMutateIncomingTransportMessages, IMutateOutgoingTransportMessages { }
}

namespace NServiceBus.MessageMutator
{
    using Unicast.Messages;
    
    [ObsoleteEx(
                ReplacementTypeOrMember = "IMutateOutgoingPhysicalContext",
                RemoveInVersion = "7.0",
                TreatAsErrorFromVersion = "6.0")]
    public interface IMutateOutgoingTransportMessages
    {
        void MutateOutgoing(LogicalMessage logicalMessage, TransportMessage transportMessage);
    }
}


namespace NServiceBus.Unicast
{

    using System;
    public partial class UnicastBus
    {   
        /// <summary>
        /// Obsoleted
        /// </summary>
        /// <param name="address"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        [ObsoleteEx(
            ReplacementTypeOrMember = "Send(string destination, object message)", 
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback Send(Address address, object message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Obsoleted
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <param name="messageConstructor"></param>
        /// <returns></returns>
        [ObsoleteEx(
            ReplacementTypeOrMember = "Send<T>(string destination, Action<T> messageConstructor)", 
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        // ReSharper disable UnusedParameter.Global
        public ICallback Send<T>(Address address, Action<T> messageConstructor)
        // ReSharper restore UnusedParameter.Global
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Obsoleted
        /// </summary>
        /// <param name="address"></param>
        /// <param name="correlationId"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        [ObsoleteEx(
            ReplacementTypeOrMember = "Send<T>(string destination, string correlationId, object message)", 
            RemoveInVersion = "7.0", 
            TreatAsErrorFromVersion = "6.0")]
        // ReSharper disable UnusedParameter.Global
        public ICallback Send(Address address, string correlationId, object message)
        // ReSharper restore UnusedParameter.Global
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Obsolete
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <param name="correlationId"></param>
        /// <param name="messageConstructor"></param>
        /// <returns></returns>
        [ObsoleteEx(
            ReplacementTypeOrMember = "Send<T>(string destination, string correlationId, Action<T> messageConstructor)", 
            RemoveInVersion = "7.0", 
            TreatAsErrorFromVersion = "6.0")]
        // ReSharper disable UnusedParameter.Global
        public ICallback Send<T>(Address address, string correlationId, Action<T> messageConstructor)
        // ReSharper restore UnusedParameter.Global
        {
            throw new NotImplementedException();
        }

     }
}
