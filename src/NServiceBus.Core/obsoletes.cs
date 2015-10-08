
#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global

/* Types not obsoleted since they would clash with the new `NServiceBus.Saga` type
- `NServiceBus.Saga.ContainSagaData`
- `NServiceBus.Saga.IAmStartedByMessages<T>`
- `NServiceBus.Saga.IConfigureHowToFindSagaWithMessage`
- `NServiceBus.Saga.IContainSagaData`
- `NServiceBus.Saga.IFinder`
- `NServiceBus.Saga.IFindSagas<T>`
- `NServiceBus.Saga.IHandleSagaNotFound`
- `NServiceBus.Saga.IHandleTimeouts<T>`
- `NServiceBus.Saga.ISagaPersister`
- `NServiceBus.Saga.Saga`
- `NServiceBus.Saga.Saga<TSagaData>`
- `NServiceBus.Saga.SagaPropertyMapper<TSagaData>`
- `NServiceBus.Saga.ToSagaExpression<TSagaData, TMessage>`
- `NServiceBus.Saga.UniqueAttribute`
*/

/* Types moved to the host package
- `NServiceBus.EndpointNameAttribute`
- `NServiceBus.EndpointSLAAttribute`
- `NServiceBus.IConfigureThisEndpoint`
*/

namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "Replaced by NServiceBus.Callbacks package")]
    public interface ICallback
    {
    }

    [ObsoleteEx(
        Message = "Use the string based overloads",
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0")]
    public class Address
    {
    }

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "Replaced by NServiceBus.Callbacks package")]
    public class CompletionResult
    {
    }

    [ObsoleteEx(
        RemoveInVersion = "7",
        TreatAsErrorFromVersion = "6")]
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

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "BusConfiguration.ExecuteTheseHandlersFirst")]
    public interface ISpecifyMessageHandlerOrdering
    {
        void SpecifyOrder(Order order);
    }

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "BusConfiguration.ExecuteTheseHandlersFirst")]
    public class First<T>
    {
    }

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "BusConfiguration.ExecuteTheseHandlersFirst")]
    public class Order
    {
        public IEnumerable<Type> Types
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public void SpecifyFirst<T>()
        {
            throw new NotImplementedException();
        }

        public void Specify<T>(First<T> ordering)
        {
            throw new NotImplementedException();
        }

        public void Specify(params Type[] priorityHandlers)
        {
            throw new NotImplementedException();
        }
    }


    public partial class BusConfiguration
    {
        [ObsoleteEx(
            ReplacementTypeOrMember = "SetOutgoingHeaders(string key,string value)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public IDictionary<string, string> OutgoingHeaders
        {
            get { throw new NotImplementedException(); }
        }

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
            ReplacementTypeOrMember = "UseCustomLogicalToTransportAddressTranslation",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public void OverrideLocalAddress(string queue)
        {
            throw new NotImplementedException();
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
            get { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "ReadOnlySettings.LocalAddress()")]
        public string LocalAddress
        {
            get { throw new NotImplementedException(); }
        }
    }

    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        ReplacementTypeOrMember = "BusConfiguration.ExcludeAssemblies")]
    public class AllAssemblies : IExcludesBuilder, IIncludesBuilder
    {
    }

    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7")]
    public interface IExcludesBuilder
    {
    }

    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7")]
    public interface IIncludesBuilder
    {
    }

    public class TransportMessage
    {
        [ObsoleteEx(
            Message = "Not used anymore, you most likely should use a `OutgoingMessage` instead",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public TransportMessage()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "For sending purposes use DeliveryMessageOptions.NonDurable (note the negation). When receiving look at the new 'NServiceBus.NonDurableMessage' header",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public bool Recoverable
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "For sending purposes use DeliveryMessageOptions.TimeToBeReceived. When receiving look at the new 'NServiceBus.TimeToBeReceived' header",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public TimeSpan TimeToBeReceived
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }


        [ObsoleteEx(
            Message = "Use the value of the 'NServiceBus.CorrelationId' header instead",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public string CorrelationId
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        ///     Gets/sets a byte array to the body content of the message.
        /// </summary>
        [ObsoleteEx(
            Message = "Use the value of the 'IncomingMessage.Body' instead",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public byte[] Body { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        [ObsoleteEx(
          Message = "Not used anymore, you most likely should use a `IncomingMessage` instead",
          RemoveInVersion = "7.0",
          TreatAsErrorFromVersion = "6.0")]
        public TransportMessage(string existingId, Dictionary<string, string> existingHeaders)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets/sets the identifier of this message bundle.
        /// </summary>
        [ObsoleteEx(
            Message = "Use the value of the 'IncomingMessage.MessageId' or 'OutgoingMesssage.MessageId' instead",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public string Id { get { throw new NotImplementedException(); } }

        /// <summary>
        ///     Gets/sets the reply-to address of the message bundle - replaces 'ReturnAddress'.
        /// </summary>
        [ObsoleteEx(
            ReplacementTypeOrMember = "GetReplyToAddress(this IncomingMessage message)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public string ReplyToAddress { get { throw new NotImplementedException(); } }

        /// <summary>
        ///     Indicates to the infrastructure the message intent (publish, or regular send).
        /// </summary>
        [ObsoleteEx(
            ReplacementTypeOrMember = "GetMessageIntent(this IncomingMessage message)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public MessageIntentEnum MessageIntent { get { throw new NotImplementedException(); } }

        /// <summary>
        ///     Gets/sets other applicative out-of-band information.
        /// </summary>
        [ObsoleteEx(
            Message = "Use the value of the 'IncomingMessage.Headers' or 'OutgoingMesssage.Headers' instead",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();

    }

    public partial interface IBus
    {
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Replaced by NServiceBus.Callbacks package")]
        void Return<T>(T errorEnum);

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "SendLocalAsync(object message, SendLocalOptions options)")]
        ICallback Defer(TimeSpan delay, object message);

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "SendLocalAsync(object message, SendLocalOptions options)")]
        ICallback Defer(DateTime processAt, object message);
    }
}

namespace NServiceBus.Unicast
{
    using System;

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "Not a public API",
        ReplacementTypeOrMember = "MessageHandlerRegistry")]
    public interface IMessageHandlerRegistry
    {
    }

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
        public string ReplyToAddress
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "Turn best practices check off using configuration.DisableFeature<BestPracticeEnforcement>()",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public bool EnforceMessagingBestPractices
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
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
    using Unicast;

    public partial class TimeoutData
    {
        [ObsoleteEx(
            Message = "Not used anymore",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public const string OriginalReplyToAddress = "NServiceBus.Timeout.ReplyToAddress";

        [ObsoleteEx(
            Message = "Use new OutgoingMessage(timeoutData.State) instead",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
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
    public interface IMessageMutator : IMutateOutgoingMessages, IMutateIncomingMessages
    {
    }

    [ObsoleteEx(
        Message = "Just have your mutator implement both IMutateIncomingTransportMessages and IMutateOutgoingTransportMessages",
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0")]
    public interface IMutateTransportMessages : IMutateIncomingTransportMessages, IMutateOutgoingTransportMessages
    {
    }
}

namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Hosting;
    using ObjectBuilder;
    using Settings;

    partial class ContextualBus
    {
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "No longer used")]
        public bool PropagateReturnAddressOnSend
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "SendLocalAsync(object message, SendLocalOptions options)")]
        public ICallback Defer(TimeSpan delay, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "SendLocalAsync(object message, SendLocalOptions options)")]
        public ICallback Defer(DateTime processAt, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Replaced by NServiceBus.Callbacks package")]
        public void Return<T>(T errorCode)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback SendAsync(Address address, object message)
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
        public ICallback SendAsync<T>(Address address, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback SendAsync(string destination, string correlationId, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback SendAsync(Address address, string correlationId, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback SendAsync<T>(string destination, string correlationId, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback SendAsync<T>(Address address, string correlationId, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }
    }

    [ObsoleteEx(
        Message = "UnicastBus has been made internal. Use either IBus or ISendOnlyBus.",
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7")]
    public class UnicastBus : IStartableBus
    {
        UnicastBus()
        {
        }

        [ObsoleteEx(
            Message = "We have introduced a more explicit API to set the host identifier, see busConfiguration.UniquelyIdentifyRunningInstance()",
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7")]
        public HostInformation HostInformation
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "ReadOnlySettings should be accessed inside feature, the pipeline and start/stop infrastructure only.",
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7")]
        public ReadOnlySettings Settings
        {
            get { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "Builder should be accessed inside feature, the pipeline and start/stop infrastructure only.",
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7")]
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
            get { throw new NotImplementedException(); }
        }


        [ObsoleteEx(
            ReplacementTypeOrMember = "ISendOnlyBus.PublishAsync(object message, PublishOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public Task PublishAsync(object message, NServiceBus.PublishOptions options)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "ISendOnlyBus.PublishAsync<T>(Action<T> messageConstructor, PublishOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public Task PublishAsync<T>(Action<T> messageConstructor, NServiceBus.PublishOptions publishOptions)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "ISendOnlyBus.SendAsync(object message, SendOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public Task SendAsync(object message, NServiceBus.SendOptions options)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "ISendOnlyBus.SendAsync<T>(Action<T> messageConstructor, SendOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public Task SendAsync<T>(Action<T> messageConstructor, NServiceBus.SendOptions options)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "SendLocalAsync(object message, SendLocalOptions options)")]
        public ICallback Defer(TimeSpan delay, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "SendLocalAsync(object message, SendLocalOptions options)")]
        public ICallback Defer(DateTime processAt, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "IBus.ReplyAsync<T>(object message, ReplyOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public Task ReplyAsync(object message, NServiceBus.ReplyOptions options)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "IBus.ReplyAsync<T>(Action<T> messageConstructor, ReplyOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public Task ReplyAsync<T>(Action<T> messageConstructor, NServiceBus.ReplyOptions options)
        {
            throw new NotImplementedException();
        }

        public Task SubscribeAsync(Type eventType, SubscribeOptions options)
        {
            throw new NotImplementedException();
        }

        public Task UnsubscribeAsync(Type eventType, UnsubscribeOptions options)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "IBus.HandleCurrentMessageLaterAsync()",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public Task HandleCurrentMessageLaterAsync()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "IBus.ForwardCurrentMessageToAsync(string destination)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public Task ForwardCurrentMessageToAsync(string destination)
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
            ReplacementTypeOrMember = "IStartableBus.StartAsync()",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public Task<IBus> StartAsync()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Replaced by NServiceBus.Callbacks package")]
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
    }

    public partial class MessageHandlerRegistry
    {
        [ObsoleteEx(ReplacementTypeOrMember = "MessageHandlerRegistry.GetHandlersFor(Type messageType)", RemoveInVersion = "7", TreatAsErrorFromVersion = "6")]
        public IEnumerable<Type> GetHandlerTypes(Type messageType)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(ReplacementTypeOrMember = "MessageHandler.Invoke(object message, object context)", RemoveInVersion = "7", TreatAsErrorFromVersion = "6")]
        public void InvokeHandle(object handler, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(ReplacementTypeOrMember = "MessageHandler.Invoke(object message, object context)", RemoveInVersion = "7", TreatAsErrorFromVersion = "6")]
        public void InvokeTimeout(object handler, object state)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(ReplacementTypeOrMember = "MessageHandlerRegistry.RegisterHandler(Type handlerType)", RemoveInVersion = "7", TreatAsErrorFromVersion = "6")]
        public void CacheMethodForHandler(Type handler, Type messageType)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Unicast.Behaviors
{
    using System;

    public partial class MessageHandler
    {
        [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", ReplacementTypeOrMember = "MessageHandler(Action<object, object, object> invocation, Type handlerType)")]
        public MessageHandler()
        {
            throw new NotImplementedException("Creator of the message handler must assign the handler type and the invocation delegate");
        }

        [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", ReplacementTypeOrMember = "MessageHandler.Invoke")]
        public Action<object, object> Invocation
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }
}

namespace NServiceBus.Unicast.Messages
{
    using System;

    public partial class MessageMetadata
    {
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "You can access TTBR via the DeliveryConstraints collection on the outgoing context")]
        public TimeSpan TimeToBeReceived
        {
            get { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "You can access Recoverable via the DeliveryConstraints collection on the outgoing context, the new constraint is called NonDurableDelivery")]
        public bool Recoverable
        {
            get { throw new NotImplementedException(); }
        }
    }
}

namespace NServiceBus
{
    using System;

    public partial class Conventions
    {
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "No longer an extension point")]
        public TimeSpan GetTimeToBeReceived(Type messageType)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "No longer an extension point")]
        public static bool IsExpressMessageType(Type t)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus
{
    using System;

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "Headers are not managed via the send, reply and publishoptions")]
    public static class ExtensionMethods
    {
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use a incoming behavior to get access to the current message")]
        public static object CurrentMessageBeingHandled
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Headers are not 'set' only on the outgoing pipeline")]
        public static string GetMessageHeader(this IBus bus, object msg, string key)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Headers can be set using the ``.SetHeader` method on the context object passed into your behavior or mutator")]
        public static void SetMessageHeader(this ISendOnlyBus bus, object msg, string key, string value)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Messaging;

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "The msmq transaction is now available via the pipeline context")]
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
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use the ConsistencyGuarantee class instead")]
        public bool EnlistInReceiveTransaction
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use context.TryGetDeliveryConstraint<DiscardIfNotReceivedBefore> instead")]
        public TimeSpan? TimeToBeReceived
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use context.TryGetDeliveryConstraint<NonDurableDelivery> instead")]
        public bool? NonDurable
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }
}

namespace NServiceBus.Features
{
    using System;

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "No longer used, safe to remove")]
    public class StorageDrivenPublishing : Feature
    {
        internal StorageDrivenPublishing()
        {
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            throw new NotImplementedException();
        }
    }

    public static class SerializationFeatureHelper
    {
    }
}

namespace NServiceBus.Transports
{
    using Unicast;

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "IDispatchMessages")]
    public interface IDeferMessages
    {
        void Defer(TransportMessage message, SendOptions sendOptions);


        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "ICancelDeferredMessages")]
        void ClearDeferredMessages(string headerKey, string headerValue);
    }
}

namespace NServiceBus.Transports
{
    using Unicast;

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "IDispatchMessages")]
    public interface IPublishMessages
    {
        void Publish(TransportMessage message, PublishOptions publishOptions);
    }
}

namespace NServiceBus.Transports
{
    using Unicast;

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "IDispatchMessages")]
    public interface ISendMessages
    {
        void Send(TransportMessage message, SendOptions sendOptions);
    }
}

namespace NServiceBus.Features
{
    using System;

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "No longer used, safe to remove")]
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
    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "No longer used, safe to remove")]
    public interface IAuditMessages
    {
    }
}

namespace NServiceBus.Unicast.Subscriptions
{
    using System;

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "No longer used, safe to remove")]
    public class SubscriptionEventArgs : EventArgs
    {
        public string SubscriberReturnAddress { get; set; }

        public string MessageType { get; set; }
    }
}

namespace NServiceBus.Unicast.Routing
{
    using System;
    using System.Collections.Generic;

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "No longer used, safe to remove")]
    public class StaticMessageRouter
    {
        public StaticMessageRouter(IEnumerable<Type> knownMessages)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "config.AutoSubscribe().AutoSubscribePlainMessages()")]
        public bool SubscribeToPlainMessages { get; set; }

        public List<string> GetDestinationFor(Type messageType)
        {
            throw new NotImplementedException();
        }

        public void RegisterEventRoute(Type eventType, string endpointAddress)
        {
            throw new NotImplementedException();
        }

        public void RegisterMessageRoute(Type messageType, string endpointAddress)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.AutomaticSubscriptions.Config
{
    using System;

    public partial class AutoSubscribeSettings
    {
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            Message = "Transports with support for centralized pubsub will default this to true. Can safely be removed")]
        public void DoNotRequireExplicitRouting()
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Config
{
    [ObsoleteEx(
        Message = "Use the feature concept instead via A class which inherits from `NServiceBus.Features.Feature` and use `configuration.EnableFeature<YourClass>()`",
        RemoveInVersion = "7",
        TreatAsErrorFromVersion = "6")]
    public interface IWantToRunWhenConfigurationIsComplete
    {
        void Run(Configure config);
    }
}

namespace NServiceBus.Faults
{
    [ObsoleteEx(
        Message = "IManageMessageFailures is no longer an extension point. If you want full control over what happens when a message fails (including retries) please override the MoveFaultsToErrorQueue behavior. If you just want to get notified when messages are being moved please use BusNotifications.Errors.MessageSentToErrorQueue.Subscribe(e=>{}) ",
        RemoveInVersion = "7",
        TreatAsErrorFromVersion = "6")]
    public interface IManageMessageFailures
    {
    }
}

namespace NServiceBus.Sagas
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "There is no need for this attribute anymore, all mapped properties are automatically correlated.")]
    public sealed class UniqueAttribute : Attribute
    {
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use the new SagaMetadata")]
        public static PropertyInfo GetUniqueProperty(Type type)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use the new SagaMetadata")]
        public static KeyValuePair<string, object>? GetUniqueProperty(IContainSagaData entity)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use the new SagaMetadata")]
        public static IDictionary<string, object> GetUniqueProperties(IContainSagaData entity)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use the new SagaMetadata")]
        public static IEnumerable<PropertyInfo> GetUniqueProperties(Type type)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Persistence
{
    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "NServiceBus.Persistence.StorageType")]
    public enum Storage
    {
        Timeouts = 1,
        Subscriptions = 2,
        Sagas = 3,
        GatewayDeduplication = 4,
        Outbox = 5
    }
}

namespace NServiceBus.Unicast.Queuing
{
    [ObsoleteEx(
        ReplacementTypeOrMember = "QueueBindings",
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0")]
    public interface IWantQueueCreated
    {
    }
}

namespace NServiceBus.Transports
{
    using System;
    using Unicast.Transport;

    [ObsoleteEx(
        ReplacementTypeOrMember = "NServiceBus.Transport.IPushMessages",
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0")]
    public interface IDequeueMessages
    {
        void Init(Address address, TransactionSettings transactionSettings, Func<TransportMessage, bool> tryProcessMessage, Action<TransportMessage, Exception> endProcessMessage);
        void Start(int maximumConcurrencyLevel);
        void Stop();
    }
}

namespace NServiceBus.Transports.Msmq
{
    using System;
    using Unicast.Transport;

    [ObsoleteEx(
        Message = "No longer available, resolve an instance of IPushMessages from the container instead",
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0")]
    public class MsmqDequeueStrategy : IDequeueMessages, IDisposable
    {
        public MsmqDequeueStrategy(Configure configure, CriticalError criticalError, MsmqUnitOfWork unitOfWork)
        {
        }


        public Address ErrorQueue { get; set; }


        public void Init(Address address, TransactionSettings settings, Func<TransportMessage, bool> tryProcessMessage, Action<TransportMessage, Exception> endProcessMessage)
        {
        }

        public void Start(int maximumConcurrencyLevel)
        {
        }

        public void Stop()
        {
        }

        public void Dispose()
        {
        }
    }
}

namespace NServiceBus.Unicast.Transport
{
    using System;
    using System.Transactions;

    public partial class TransactionSettings
    {
        [ObsoleteEx(
            Message = "No longer used",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public TransactionSettings(bool isTransactional, TimeSpan transactionTimeout, IsolationLevel isolationLevel, bool suppressDistributedTransactions, bool doNotWrapHandlersExecutionInATransactionScope)
        {
        }

        [ObsoleteEx(
            Message = "DoNotWrapHandlersExecutionInATransactionScope is no longer used here. Please use settings.GetOrDefault<bool>('Transactions.DoNotWrapHandlersExecutionInATransactionScope') instead",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public bool DoNotWrapHandlersExecutionInATransactionScope { get; set; }
    }

    /// <summary>
    /// Defines the event data for the failed message processing event.
    /// </summary>
    [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "Use the pipeline to catch failures")]
    public class FailedMessageProcessingEventArgs : EventArgs
    {
    }
}

namespace NServiceBus.Settings
{
    using System;

    public partial class ScaleOutSettings
    {
        [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", Message = "This is the default starting with V6.")]
        public void UseSingleBrokerQueue()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", ReplacementTypeOrMember = "UniqueQueuePerEndpointInstance")]
        public void UseUniqueBrokerQueuePerMachine()
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Routing.StorageDrivenPublishing
{
    using System;
    using System.Collections.Generic;

    [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", Message = "No longer an extension point, if you want to list events without subscribers you can take a dependency on ISubscriptionStorage and query it for the event types you want to check")]
    public class SubscribersForEvent
    {
        public SubscribersForEvent(List<string> subscribers, Type eventType)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> Subscribers { get; private set; }

        public Type EventType { get; private set; }
    }
}

namespace NServiceBus
{
    using System;
    using Unicast;

    [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", Message = "No longer used, please use the new callbacks api described in the v6 upgrade guide")]
    public class BusAsyncResultEventArgs : EventArgs
    {
        public BusAsyncResult Result { get; set; }

        public string MessageId { get; set; }
    }
}

namespace NServiceBus.Unicast
{
    using System;
    using System.Threading;

    [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", Message = "No longer used, please use the new callbacks api described in the v6 upgrade guide")]
    public class BusAsyncResult : IAsyncResult
    {
        public BusAsyncResult(AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public object AsyncState
        {
            get { throw new NotImplementedException(); }
        }

        public WaitHandle AsyncWaitHandle
        {
            get { throw new NotImplementedException(); }
        }

        public bool CompletedSynchronously
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsCompleted
        {
            get { throw new NotImplementedException(); }
        }

        public void Complete(int errorCode, params object[] messages)
        {
            throw new NotImplementedException();
        }
    }
}


namespace NServiceBus
{
    using System;

    [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", Message = "No longer used, can safey be removed")]
    public interface IManageMessageHeaders
    {
        Action<object, string, string> SetHeaderAction { get; }
        Func<object, string, string> GetHeaderAction { get; }
    }
}
namespace NServiceBus.Pipeline.Contexts
{
    using System;
    using Unicast;
    using Unicast.Messages;

    [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", ReplacementTypeOrMember = "OutgoingLogicalMessage")]
    public class OutgoingContext : BehaviorContext
    {
        public OutgoingContext(BehaviorContext parentContext, DeliveryOptions deliveryOptions, LogicalMessage message)
             : base(parentContext)
        {
            throw new NotImplementedException();
        }

        public DeliveryOptions DeliveryOptions
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Outgoing logical message.
        /// </summary>
        public LogicalMessage OutgoingLogicalMessage
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// The received message, if any.
        /// </summary>
        public TransportMessage IncomingMessage
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// The message about to be sent out.
        /// </summary>
        public TransportMessage OutgoingMessage
        {
            get { throw new NotImplementedException(); }
        }

    }
}

namespace NServiceBus.Pipeline
{
    using System;

    [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", ReplacementTypeOrMember = "Behavior<T>")]
    public interface IBehavior<in TContext> where TContext : BehaviorContext
    {
        /// <summary>
        /// Called when the behavior is executed.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="next">The next <see cref="IBehavior{TContext}"/> in the chain to execute.</param>
        void Invoke(TContext context, Action next);
    }
}

namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using ObjectBuilder;
    using Settings;

    [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", Message = "You can no longer get access to the pipeline context via DI. Please use a behavior to get access instead")]
    public class PipelineExecutor : IDisposable
    {
        public PipelineExecutor(ReadOnlySettings settings, IBuilder builder, BusNotifications busNotifications)
        {
            throw new NotImplementedException();
        }
        public IList<RegisterStep> Incoming { get; private set; }

        public IList<RegisterStep> Outgoing { get; private set; }
        public BehaviorContext CurrentContext
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void Dispose()
        {
            //Injected
        }
        public void InvokePipeline<TContext>(IEnumerable<Type> behaviors, TContext context) where TContext : BehaviorContext
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Satellites
{
    [ObsoleteEx(
       Message = "ISatellite is no longer an extension point. In order to create a satellite one must create a feature that uses AddSatellitePipeline() method and a class that inherits from SatelliteBehavior that is used for processing the messages.",
       RemoveInVersion = "7",
       TreatAsErrorFromVersion = "6")]
    public interface IAdvancedSatellite
    {
    }

    [ObsoleteEx(
        Message = "ISatellite is no longer an extension point. In order to create a satellite one must create a feature that uses AddSatellitePipeline() method and a class that inherits from SatelliteBehavior that is used for processing the messages.",
        RemoveInVersion = "7",
        TreatAsErrorFromVersion = "6")]
    public interface ISatellite
    {
    }
}

namespace NServiceBus.Unicast.Transport
{
    using System;

    [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", Message = "No longer used, can safey be removed")]
    public static class ControlMessage
    {
        public static TransportMessage Create()
        {
            throw new NotImplementedException();
        }
    }
}
namespace NServiceBus.Unicast.Transport
{
    using System;

    [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", ReplacementTypeOrMember = "IPushMessages")]
    public interface ITransport
    {

        void Start(Address localAddress);

        int MaximumConcurrencyLevel { get; }

        void ChangeMaximumConcurrencyLevel(int maximumConcurrencyLevel);

        int MaximumMessageThroughputPerSecond { get; }

        void ChangeMaximumMessageThroughputPerSecond(int maximumMessageThroughputPerSecond);

        event EventHandler<TransportMessageReceivedEventArgs> TransportMessageReceived;

        event EventHandler<StartedMessageProcessingEventArgs> StartedMessageProcessing;

        event EventHandler<FinishedMessageProcessingEventArgs> FinishedMessageProcessing;

        event EventHandler<FailedMessageProcessingEventArgs> FailedMessageProcessing;

        void AbortHandlingCurrentMessage();

        void Stop();
    }
}
namespace NServiceBus.Unicast.Transport
{
    using System;


    [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", Message = "No longer used, can safey be removed")]
    public class TransportMessageReceivedEventArgs : EventArgs
    {
        public TransportMessageReceivedEventArgs(TransportMessage m)
        {
            throw new NotImplementedException();
        }

        public TransportMessage Message
        {
            get { throw new NotImplementedException(); }
        }
    }
}

namespace NServiceBus.Unicast.Transport
{
    using System;

    [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", Message = "No longer used, can safey be removed")]
    public class StartedMessageProcessingEventArgs : EventArgs
    {
        public StartedMessageProcessingEventArgs(TransportMessage m)
        {
            throw new NotImplementedException();
        }
        public TransportMessage Message
        {
            get { throw new NotImplementedException(); }
        }
    }

    [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", Message = "No longer used, can safey be removed")]
    public class FinishedMessageProcessingEventArgs : EventArgs
    {
        public FinishedMessageProcessingEventArgs(TransportMessage m)
        {
            throw new NotImplementedException();
        }
        public TransportMessage Message
        {
            get { throw new NotImplementedException(); }
        }
    }
}
namespace NServiceBus.Unicast.Transport
{
    using System;

    [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", Message = "No longer used, can safey be removed")]
    public class TransportMessageAvailableEventArgs : EventArgs
    {
        public TransportMessageAvailableEventArgs(TransportMessage m)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     The received <see cref="TransportMessage" />.
        /// </summary>
        public TransportMessage Message
        {
            get { throw new NotImplementedException(); }
        }
    }
}
