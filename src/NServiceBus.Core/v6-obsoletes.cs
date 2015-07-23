#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
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

        [ObsoleteEx(
            ReplacementTypeOrMember = "SetOutgoingHeaders(string key,string value)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public IDictionary<string, string> OutgoingHeaders
        {
            get
            {
                throw new NotImplementedException();
            }
        }

    }

    public partial class Configure
    {

        [ObsoleteEx(
            Message = "Static headers is no longer accessible via this object",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public IDictionary<string, string> OutgoingHeaders
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", ReplacementTypeOrMember = "settings.LocalAddress()")]
        public string LocalAddress
        {
            get
            {
                throw new NotImplementedException();
            }
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
            Message = "Use the value of the 'NServiceBus.CorrelationId' header instead",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public string CorrelationId { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        [ObsoleteEx(
          Message = "Not used anymore, you most likely should use a `OutgoingMessageInstead`",
          RemoveInVersion = "7.0",
          TreatAsErrorFromVersion = "6.0")]
        public TransportMessage()
        {
            throw new NotImplementedException();
        }
    }

    public partial interface IBus
    {
        /// <summary>
        /// Returns a completion message with the specified error code to the sender
        /// of the message being handled. The type T can only be an enum or an integer.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "Replaced by NServiceBus.Callbacks package")]
        void Return<T>(T errorEnum);

        /// <summary>
        /// Defers the processing of the message for the given delay. This feature is using the timeout manager so make sure that you enable timeouts.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", ReplacementTypeOrMember = "SendLocal(object message, SendLocalOptions options)")]
        // ReSharper disable UnusedParameter.Global
        ICallback Defer(TimeSpan delay, object message);
        // ReSharper restore UnusedParameter.Global

        /// <summary>
        /// Defers the processing of the message until the specified time. This feature is using the timeout manager so make sure that you enable timeouts.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", ReplacementTypeOrMember = "SendLocal(object message, SendLocalOptions options)")]
        // ReSharper disable UnusedParameter.Global
        ICallback Defer(DateTime processAt, object message);
        // ReSharper restore UnusedParameter.Global
    }

    public partial interface ISendOnlyBus
    {
        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="address">
        /// The address to which the message will be sent.
        /// </param>
        /// <param name="message">The message to send.</param>
        [ObsoleteEx(
            ReplacementTypeOrMember = "Send(object message, SendOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        ICallback Send(Address address, object message);

        /// <summary>
        /// Instantiates a message of type T and sends it to the given address.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="address">The address to which the message will be sent.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        [ObsoleteEx(
            ReplacementTypeOrMember = "Send<T>(Action<T> messageConstructor, SendOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        ICallback Send<T>(Address address, Action<T> messageConstructor);

        /// <summary>
        /// Sends the message to the destination as well as identifying this
        /// as a response to a message containing the Id found in correlationId.
        /// </summary>
        [ObsoleteEx(
            ReplacementTypeOrMember = "Send(object message, SendOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        ICallback Send(string destination, string correlationId, object message);

        /// <summary>
        /// Sends the message to the given address as well as identifying this
        /// as a response to a message containing the Id found in correlationId.
        /// </summary>
        [ObsoleteEx(
            ReplacementTypeOrMember = "Send(object message, SendOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        ICallback Send(Address address, string correlationId, object message);

        /// <summary>
        /// Instantiates a message of the type T using the given messageConstructor,
        /// and sends it to the destination identifying it as a response to a message
        /// containing the Id found in correlationId.
        /// </summary>
        [ObsoleteEx(
            ReplacementTypeOrMember = "Send<T>(Action<T> messageConstructor, SendOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        ICallback Send<T>(string destination, string correlationId, Action<T> messageConstructor);

        /// <summary>
        /// Instantiates a message of the type T using the given messageConstructor,
        /// and sends it to the given address identifying it as a response to a message
        /// containing the Id found in correlationId.
        /// </summary>
        [ObsoleteEx(
            ReplacementTypeOrMember = "Send<T>(Action<T> messageConstructor, SendOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        ICallback Send<T>(Address address, string correlationId, Action<T> messageConstructor);
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

        [ObsoleteEx(
           Message = "Turn best practices check off using configuration.DisableFeature<BestPracticeEnforcement>()",
           RemoveInVersion = "7.0",
           TreatAsErrorFromVersion = "6.0")]
        public bool EnforceMessagingBestPractices { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
    }

    [ObsoleteEx(
            Message = "Use context.Intent to detect of the message is a event being published and use context.MessageType to get the actual event type",
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

    [ObsoleteEx(
                Message = "Just have your mutator implement both IMutateIncomingTransportMessages and IMutateOutgoingPhysicalContext ",
                RemoveInVersion = "7.0",
                TreatAsErrorFromVersion = "6.0")]
    public interface IMutateTransportMessages : IMutateIncomingTransportMessages, IMutateOutgoingTransportMessages { }
}


namespace NServiceBus.Unicast
{
    using System;
    using NServiceBus.Hosting;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;

    partial class ContextualBus
    {
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback Send(Address address, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback Send<T>(string destination, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback Send<T>(Address address, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback Send(string destination, string correlationId, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback Send(Address address, string correlationId, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback Send<T>(string destination, string correlationId, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback Send<T>(Address address, string correlationId, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", ReplacementTypeOrMember = "SendLocal(object message, SendLocalOptions options)")]
        public ICallback Defer(TimeSpan delay, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", ReplacementTypeOrMember = "SendLocal(object message, SendLocalOptions options)")]
        public ICallback Defer(DateTime processAt, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "Replaced by NServiceBus.Callbacks package")]
        public void Return<T>(T errorCode)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "No longer used")]
        public bool PropagateReturnAddressOnSend
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }

    [ObsoleteEx(Message = "UnicastBus has been made internal. Use either IBus or ISendOnlyBus.", TreatAsErrorFromVersion = "6", RemoveInVersion = "7")]
    public class UnicastBus : IStartableBus
    {
        UnicastBus()
        {
        }

        [ObsoleteEx(Message = "We have introduced a more explicit API to set the host identifier, see busConfiguration.UniquelyIdentifyRunningInstance()", TreatAsErrorFromVersion = "6", RemoveInVersion = "7")]
        public HostInformation HostInformation
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(Message = "ReadOnlySettings should be accessed inside feature, the pipeline and start/stop infrastructure only.", TreatAsErrorFromVersion = "6", RemoveInVersion = "7")]
        public ReadOnlySettings Settings
        {
            get { throw new NotImplementedException(); }
        }

        [ObsoleteEx(Message = "Builder should be accessed inside feature, the pipeline and start/stop infrastructure only.", TreatAsErrorFromVersion = "6", RemoveInVersion = "7")]
        public IBuilder Builder
        {
            get { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "IBus.CurrentMessageContext",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public IMessageContext CurrentMessageContext
        {
            get {  throw new NotImplementedException(); } 
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "Send(object message, SendOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback Send(Address address, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "Send<T>(Action<T> messageConstructor, SendOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback Send<T>(Address address, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "Send(object message, SendOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback Send(string destination, string correlationId, object message)
        {
            throw new NotImplementedException();
        }

         [ObsoleteEx(
            ReplacementTypeOrMember = "Send(object message, SendOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback Send(Address address, string correlationId, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "Send<T>(Action<T> messageConstructor, SendOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback Send<T>(string destination, string correlationId, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "Send<T>(Action<T> messageConstructor, SendOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback Send<T>(Address address, string correlationId, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "ISendOnlyBus.Publish(object message, PublishOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public void Publish(object message, NServiceBus.PublishOptions options)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "ISendOnlyBus.Publish<T>(Action<T> messageConstructor, PublishOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public void Publish<T>(Action<T> messageConstructor, NServiceBus.PublishOptions publishOptions)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "ISendOnlyBus.Send(object message, SendOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public void Send(object message, NServiceBus.SendOptions options)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "ISendOnlyBus.Send<T>(Action<T> messageConstructor, SendOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public void Send<T>(Action<T> messageConstructor, NServiceBus.SendOptions options)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", ReplacementTypeOrMember = "SendLocal(object message, SendLocalOptions options)")]
        public ICallback Defer(TimeSpan delay, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", ReplacementTypeOrMember = "SendLocal(object message, SendLocalOptions options)")]
        public ICallback Defer(DateTime processAt, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "IBus.Subscribe(Type messageType)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public void Subscribe(Type messageType)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "IBus.Subscribe<T>()",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public void Subscribe<T>()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "IBus.Unsubscribe(Type messageType)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public void Unsubscribe(Type messageType)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "IBus.Unsubscribe<T>()",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public void Unsubscribe<T>()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "IBus.Reply<T>(object message, ReplyOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public void Reply(object message, NServiceBus.ReplyOptions options)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "IBus.Reply<T>(Action<T> messageConstructor, ReplyOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public void Reply<T>(Action<T> messageConstructor, NServiceBus.ReplyOptions options)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "IBus.HandleCurrentMessageLater()",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public void HandleCurrentMessageLater()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "IBus.ForwardCurrentMessageTo(string destination)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public void ForwardCurrentMessageTo(string destination)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "IBus.DoNotContinueDispatchingCurrentMessageToHandlers()",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "IStartableBus.Start()",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public IBus Start()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "Replaced by NServiceBus.Callbacks package")]
        public void Return<T>(T errorEnum)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "IBus.Dispose()",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}


namespace NServiceBus.Unicast.Messages
{
    using System;

    public partial class MessageMetadata
    {
        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "You can access TTBR via the DeliveryConstraints collection on the outgoing context")]
        public TimeSpan TimeToBeReceived
        {
            get { throw new NotImplementedException(); }
        }

        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "You can access Recoverable via the DeliveryConstraints collection on the outgoing context, the new constraint is called NonDurableDelivery")]
        public bool Recoverable { get { throw new NotImplementedException(); } }

    }
}

namespace NServiceBus
{
    using System;

    public partial class Conventions
    {
        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "No longer an extension point")]
        public TimeSpan GetTimeToBeReceived(Type messageType)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "No longer an extension point")]
        public static bool IsExpressMessageType(Type t)
        {
            throw new NotImplementedException();
        }

    }

}

namespace NServiceBus
{
    using System;

    [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "Headers are not managed via the send, reply and publishoptions")]
    public static class ExtensionMethods
    {
        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "Headers are not 'set' only on the outgoing pipeline")]
        public static string GetMessageHeader(this IBus bus, object msg, string key) { throw new NotImplementedException(); }

        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "Headers can be set using the ``.SetHeader` method on the context object passed into your behavior or mutator")]
        public static void SetMessageHeader(this ISendOnlyBus bus, object msg, string key, string value) { throw new NotImplementedException(); }

        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "Use a incoming behavior to get access to the current message")]
        public static object CurrentMessageBeingHandled { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
    }
}


namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Messaging;

    [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "The msmq transaction is now available via the pipeline context")]
    public class MsmqUnitOfWork : IDisposable
    {
        public MessageQueueTransaction Transaction
        {
            get { throw new NotImplementedException(); }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public bool HasActiveTransaction()
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Unicast
{
    using System;

    public class DeliveryMessageOptions
    {

        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "Use the ConsistencyGuarantee class instead")]
        public bool EnlistInReceiveTransaction { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "Use context.TryGetDeliveryConstraint<DiscardIfNotReceivedBefore> instead")]
        public TimeSpan? TimeToBeReceived { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "Use context.TryGetDeliveryConstraint<NonDurableDelivery> instead")]
        public bool? NonDurable { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }


    }
}

namespace NServiceBus.Features
{
    using System;

    [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "No longer used, safe to remove")]
    public class StorageDrivenPublishing : Feature
    {
        internal StorageDrivenPublishing()
        {
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Transports
{
    using NServiceBus.Unicast;

    [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", ReplacementTypeOrMember = "IDispatchMessages")]
    public interface IDeferMessages
    {
        void Defer(TransportMessage message, SendOptions sendOptions);


        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", ReplacementTypeOrMember = "ICancelDeferredMessages")]
        void ClearDeferredMessages(string headerKey, string headerValue);
    }
}

namespace NServiceBus.Transports
{
    using NServiceBus.Unicast;

    [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", ReplacementTypeOrMember = "IDispatchMessages")]
    public interface IPublishMessages
    {
        void Publish(TransportMessage message, PublishOptions publishOptions);
    }
}

namespace NServiceBus.Transports
{
    using NServiceBus.Unicast;

    [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", ReplacementTypeOrMember = "IDispatchMessages")]
    public interface ISendMessages
    {
        void Send(TransportMessage message, SendOptions sendOptions);
    }
}


namespace NServiceBus.Features
{
    using System;

    [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "No longer used, safe to remove")]
    public class TimeoutManagerBasedDeferral : Feature
    {
        internal TimeoutManagerBasedDeferral()
        {
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            throw new NotImplementedException();
        }

    }
}

namespace NServiceBus.Transports
{
    [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "No longer used, safe to remove")]
    public interface IAuditMessages
    {

    }
}

namespace NServiceBus.Unicast.Subscriptions
{
    using System;

    [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "No longer used, safe to remove")]
    public class SubscriptionEventArgs : EventArgs
    {
        public string SubscriberReturnAddress { get; set; }

        public string MessageType { get; set; }
    }
}