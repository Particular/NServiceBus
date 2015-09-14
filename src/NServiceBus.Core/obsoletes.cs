#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
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
            get
            {
                throw new NotImplementedException();
            }
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "ReadOnlySettings.LocalAddress()")]
        public string LocalAddress
        {
            get
            {
                throw new NotImplementedException();
            }
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
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Replaced by NServiceBus.Callbacks package")]
        void Return<T>(T errorEnum);

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "SendLocal(object message, SendLocalOptions options)")]
        ICallback Defer(TimeSpan delay, object message);

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "SendLocal(object message, SendLocalOptions options)")]
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
    using NServiceBus.Unicast;

    public partial class TimeoutData
    {
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
    using NServiceBus.Hosting;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;

    partial class ContextualBus
    {
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

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "SendLocal(object message, SendLocalOptions options)")]
        public ICallback Defer(TimeSpan delay, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "SendLocal(object message, SendLocalOptions options)")]
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
            TreatAsErrorFromVersion = "6.0",
            Message = "No longer used")]
        public bool PropagateReturnAddressOnSend
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
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
            ReplacementTypeOrMember = "SendLocal(object message, SendLocalOptions options)")]
        public ICallback Defer(TimeSpan delay, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "SendLocal(object message, SendLocalOptions options)")]
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

        public void Subscribe(Type eventType, SubscribeOptions options)
        {
            throw new NotImplementedException();
        }

        public void Unsubscribe(Type eventType, UnsubscribeOptions options)
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
            ReplacementTypeOrMember = "IStartableBus.Start()",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public IBus Start()
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
        public bool Recoverable { get { throw new NotImplementedException(); } }

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

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use a incoming behavior to get access to the current message")]
        public static object CurrentMessageBeingHandled
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
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
        public bool EnlistInReceiveTransaction { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use context.TryGetDeliveryConstraint<DiscardIfNotReceivedBefore> instead")]
        public TimeSpan? TimeToBeReceived { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use context.TryGetDeliveryConstraint<NonDurableDelivery> instead")]
        public bool? NonDurable { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }


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
    using NServiceBus.Unicast;

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
    using NServiceBus.Unicast;

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
    using NServiceBus.Unicast;

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

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "config.AutoSubscribe().AutoSubscribePlainMessages()")]
        public bool SubscribeToPlainMessages { get; set; }
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
        Outbox = 5,
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
    using NServiceBus.Unicast.Transport;

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
    using NServiceBus.Unicast.Transport;

    [ObsoleteEx(
     Message = "No longer available, resolve an instance of IPushMessages from the container instead",
     RemoveInVersion = "7.0",
     TreatAsErrorFromVersion = "6.0")]
    public class MsmqDequeueStrategy : IDequeueMessages, IDisposable
    {

        public MsmqDequeueStrategy(Configure configure, CriticalError criticalError, MsmqUnitOfWork unitOfWork)
        {
        }


        public void Init(Address address, TransactionSettings settings, Func<TransportMessage, bool> tryProcessMessage, Action<TransportMessage, Exception> endProcessMessage)
        {
        }


        public Address ErrorQueue { get; set; }

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
