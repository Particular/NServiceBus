// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedParameter.Local



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
    using NServiceBus.Encryption;
    using NServiceBus.ObjectBuilder;

    public static partial class ConfigureCriticalErrorAction
    {
        [ObsoleteEx(
            RemoveInVersion = "7",
            TreatAsErrorFromVersion = "6",
            ReplacementTypeOrMember = "ConfigureCriticalErrorAction.DefineCriticalErrorAction(EndpointConfiguration, Func<ICriticalErrorContext, Task>)")]
        public static void DefineCriticalErrorAction(this EndpointConfiguration endpointConfiguration, Action<string, Exception> onCriticalError)
        {
        }
    }

    [ObsoleteEx(
        Message = "Please use `IMessageHandlerContext` provided to message handlers instead.",
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0")]
    public interface IMessageContext
    {
    }

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
        public static void DiscardFailedMessagesInsteadOfSendingToErrorQueue(this EndpointConfiguration config)
        {
            throw new NotImplementedException();
        }
    }

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "EndpointConfiguration.ExecuteTheseHandlersFirst")]
    public interface ISpecifyMessageHandlerOrdering
    {
    }

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "EndpointConfiguration.ExecuteTheseHandlersFirst")]
    public class First<T>
    {
    }

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "EndpointConfiguration.ExecuteTheseHandlersFirst")]
    public class Order
    {
    }

    [ObsoleteEx(
        ReplacementTypeOrMember = "EndpointConfiguration",
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0")]
    public class BusConfiguration { }

    public partial class EndpointConfiguration
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
            ReplacementTypeOrMember = "UseTransport<T>().AddAddressTranslationRule",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public void OverrideLocalAddress(string queue)
        {
            throw new NotImplementedException();
        }
    }

    [ObsoleteEx(
        Message = "This is no longer a public API",
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0")]
    public class Configure
    {
    }

    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        ReplacementTypeOrMember = "EndpointConfiguration.ExcludeAssemblies")]
    public class AllAssemblies
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

    [ObsoleteEx(
        Message = "Not used anymore, use `OutgoingMessage` or `IncomingMessage` instead",
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0")]
    public class TransportMessage
    {
        [ObsoleteEx(
            Message = "For sending purposes use `DeliveryConstraintContextExtensions.AddDeliveryConstraint(new NonDurableDelivery())` to set NonDurable delivery or `NonDurableDelivery constraint;DeliveryConstraintContextExtensions.TryGetDeliveryConstraint(out constraint)` to read wether NonDurable delivery is set. When receiving look at the new 'NServiceBus.NonDurableMessage' header",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public bool Recoverable
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "For sending purposes use `DeliveryConstraintContextExtensions.AddDeliveryConstraint(new DiscardIfNotReceivedBefore(timeToBeReceived))` to set the `TimeToBeReceived` or `DiscardIfNotReceivedBefore constraint;DeliveryConstraintContextExtensions.TryGetDeliveryConstraint(out constraint)` to read the `TimeToBeReceived`. When receiving look at the new 'NServiceBus.TimeToBeReceived' header",
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

        [ObsoleteEx(
            Message = "Use the value of the 'IncomingMessage.Body' or 'OutgoingMessage.Body' instead",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public byte[] Body
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "Use the value of the 'IncomingMessage.MessageId' or 'OutgoingMesssage.MessageId' instead",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public string Id
        {
            get { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "GetReplyToAddress(this IncomingMessage message)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public string ReplyToAddress
        {
            get { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "GetMessageIntent(this IncomingMessage message)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public MessageIntentEnum MessageIntent
        {
            get { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            Message = "Use the value of the 'IncomingMessage.Headers' or 'OutgoingMesssage.Headers' instead",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();
    }

    public partial class BusNotifications
    {
        [ObsoleteEx(Message = "For performance reasons it is no longer possible to instrument the pipeline execution", RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0")]
        public PipelineNotifications Pipeline
        {
            get { throw new NotImplementedException(); }
        }
    }

    [ObsoleteEx(Message = "For performance reasons it is no longer possible to instrument the pipeline execution", RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0")]
    public class PipelineNotifications
    {
    }

    public static partial class ConfigureRijndaelEncryptionService
    {
        [ObsoleteEx(ReplacementTypeOrMember = "RegisterEncryptionService(this EndpointConfiguration config, Func<IEncryptionService> func)", RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "It is no longer possible to access the builder to create an encryption service. If you require container access use your container directly in the factory.")]
        public static void RegisterEncryptionService(this EndpointConfiguration config, Func<IBuilder, IEncryptionService> func)
        {
            throw new NotImplementedException();
        }
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

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0")]
    public class MessageContext
    {
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
    public class ReplyOptions
    {
    }
}

namespace NServiceBus.MessageMutator
{
    [ObsoleteEx(
        Message = "Just have your mutator implement both IMutateOutgoingMessages and IMutateIncomingMessages ",
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0")]
    public interface IMessageMutator
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

    [ObsoleteEx(
        Message = "UnicastBus has been made internal. Use IEndpointInstance instead.",
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7")]
    public class UnicastBus
    {
    }

    public partial class MessageHandlerRegistry
    {
        [ObsoleteEx(
            ReplacementTypeOrMember = "MessageHandlerRegistry.GetHandlersFor(Type messageType)",
            RemoveInVersion = "7",
            TreatAsErrorFromVersion = "6")]
        public IEnumerable<Type> GetHandlerTypes(Type messageType)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "MessageHandler.Invoke(object message, object context)",
            RemoveInVersion = "7",
            TreatAsErrorFromVersion = "6")]
        public void InvokeHandle(object handler, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "MessageHandler.Invoke(object message, object context)",
            RemoveInVersion = "7",
            TreatAsErrorFromVersion = "6")]
        public void InvokeTimeout(object handler, object state)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "MessageHandlerRegistry.RegisterHandler(Type handlerType)",
            RemoveInVersion = "7",
            TreatAsErrorFromVersion = "6")]
        public void CacheMethodForHandler(Type handler, Type messageType)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Unicast.Behaviors
{
    using System;

    public class MessageHandler
    {
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "NServiceBus.Pipeline.MessageHandler(Action<object, object, object> invocation, Type handlerType)")]
        public MessageHandler()
        {
            throw new NotImplementedException("Creator of the message handler must assign the handler type and the invocation delegate");
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "NServiceBus.Pipeline.MessageHandler.Invoke")]
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
        public static void SetMessageHeader(this IBus bus, object msg, string key, string value)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Transports.Msmq
{
    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "The msmq transaction is now available via the pipeline context")]
    public class MsmqUnitOfWork
    {
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
    public class StorageDrivenPublishing
    {
    }

    [ObsoleteEx(
        Message = "Use the ConfigureSerialization Feature class instead",
        TreatAsErrorFromVersion = "6.0",
        RemoveInVersion = "7.0",
        ReplacementTypeOrMember = "ConfigureSerialization")]
    public static class SerializationFeatureHelper
    {
    }

    public partial class Feature
    {
        [ObsoleteEx(ReplacementTypeOrMember = "FeatureConfigurationContext.RegisterStartupTask", RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0")]
        protected void RegisterStartupTask<T>() where T : FeatureStartupTask
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
        ReplacementTypeOrMember = "IDispatchMessages")]
    public interface IDeferMessages
    {
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "ICancelDeferredMessages")]
        void ClearDeferredMessages(string headerKey, string headerValue);
    }
}

namespace NServiceBus.Transports
{
    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "IDispatchMessages")]
    public interface IPublishMessages
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
    public interface ISendMessages
    {
        void Send(TransportMessage message, SendOptions sendOptions);
    }
}

namespace NServiceBus.Features
{
    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "No longer used, safe to remove")]
    public class TimeoutManagerBasedDeferral
    {
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
    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "No longer used, safe to remove")]
    public class SubscriptionEventArgs
    {
    }
}

namespace NServiceBus.Unicast.Routing
{
    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "No longer used, safe to remove")]
    public class StaticMessageRouter
    {
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

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            Message = "Encourages bad practices. IMessageSession.Subscribe should be explicitly used.")]
        public void AutoSubscribePlainMessages()
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
    [ObsoleteEx(
        Message = "No longer available, resolve an instance of IPushMessages from the container instead",
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0")]
    public class MsmqDequeueStrategy
    {
    }
}

namespace NServiceBus.Unicast.Transport
{
    using System;
    using System.Transactions;
    [ObsoleteEx(
          Message = "Transaction settings is no longer available via this class. Please see obsoletes on individual members for further details",
          RemoveInVersion = "7.0",
          TreatAsErrorFromVersion = "6.0")]
    public class TransactionSettings
    {
        [ObsoleteEx(
            Message = "Timeouts are now controlled explicitly for the transaction scope unit of work using config.UnitOfWork().WrapHandlersInATransactionScope(timeout: X)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public TimeSpan TransactionTimeout { get; set; }

        [ObsoleteEx(
            Message = "Isolation level are now controlled explicitly for the transaction scope unit of work using config.UnitOfWork().WrapHandlersInATransactionScope(isolationlevel: X)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public IsolationLevel IsolationLevel { get; set; }

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

        [ObsoleteEx(
         Message = "SuppressDistributedTransactions is no longer used here. Please use `context.Settings.GetRequiredTransactionModeForReceives() != Transactions.TransactionScope` instead.",
         RemoveInVersion = "7.0",
         TreatAsErrorFromVersion = "6.0")]
        public bool SuppressDistributedTransactions { get; set; }

        [ObsoleteEx(
            Message = "IsTransactional is no longer used here. Please use `context.Settings.GetRequiredTransactionModeForReceives() != Transactions.None` instead.",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public bool IsTransactional { get; set; }

    }

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "Use the pipeline to catch failures")]
    public class FailedMessageProcessingEventArgs : EventArgs
    {
    }
}

namespace NServiceBus.Settings
{
    using System;

    public partial class ScaleOutSettings
    {
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            Message = "This is the default starting with V6.")]
        public void UseSingleBrokerQueue()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            Message = "Not required any more as for MSMQ this is the default behavior and for other transports the unique instance ID has to be provided.")]
        public void UseUniqueBrokerQueuePerMachine()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            Message = "Not required any more as for MSMQ this is the default behavior and for other transports the unique instance ID has to be provided.")]
        public void UniqueQueuePerEndpointInstance()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "ScaleOutSettings.InstanceDiscriminator()")]
        public void UniqueQueuePerEndpointInstance(string discriminator)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Routing.StorageDrivenPublishing
{
    using System;
    using System.Collections.Generic;

    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        Message = "No longer an extension point, if you want to list events without subscribers you can take a dependency on ISubscriptionStorage and query it for the event types you want to check")]
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
    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        Message = "No longer used, please use the new callbacks api described in the v6 upgrade guide")]
    public class BusAsyncResultEventArgs
    {
    }
}

namespace NServiceBus.Unicast
{
    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        Message = "No longer used, please use the new callbacks api described in the v6 upgrade guide")]
    public class BusAsyncResult
    {
    }
}

namespace NServiceBus
{
    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        Message = "No longer used, can safely be removed")]
    public interface IManageMessageHeaders
    {
    }
}

namespace NServiceBus.Pipeline.Contexts
{
    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        ReplacementTypeOrMember = "OutgoingLogicalMessage")]
    public class OutgoingContext
    {
    }
}

namespace NServiceBus.Pipeline
{
    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        ReplacementTypeOrMember = "Behavior<T>")]
    public interface IBehavior<in TContext>
    {
    }
}

namespace NServiceBus.Pipeline
{
    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        Message = "You can no longer get access to the pipeline context via DI. Please use a behavior to get access instead")]
    public class PipelineExecutor
    {
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
    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        Message = "No longer used, can safely be removed")]
    public static class ControlMessage
    {
    }
}

namespace NServiceBus.Unicast.Transport
{
    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        ReplacementTypeOrMember = "IPushMessages")]
    public interface ITransport
    {
    }
}

namespace NServiceBus.Unicast.Transport
{
    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        Message = "No longer used, can safely be removed")]
    public class TransportMessageReceivedEventArgs
    {
    }
}

namespace NServiceBus.Unicast.Transport
{
    using System;

    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        Message = "No longer used, can safely be removed")]
    public class StartedMessageProcessingEventArgs
    {
    }

    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        Message = "No longer used, can safely be removed")]
    public class FinishedMessageProcessingEventArgs : EventArgs
    {
    }
}

namespace NServiceBus.Unicast.Transport
{
    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        Message = "No longer used, can safely be removed")]
    public class TransportMessageAvailableEventArgs
    {
    }
}

namespace NServiceBus.Transports
{
    using System;

    public abstract partial class TransportDefinition
    {
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            Message = "GetSupportedTransactionMode")]
        public bool? HasSupportForDistributedTransactions
        {
            get { throw new NotImplementedException(); }
            protected set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "GetSupportedTransactionMode")]
        public bool HasSupportForMultiQueueNativeTransactions
        {
            get { throw new NotImplementedException(); }
            protected set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "GetOutboundRoutingPolicy")]
        public bool HasNativePubSubSupport
        {
            get { throw new NotImplementedException(); }
            protected set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "GetOutboundRoutingPolicy")]
        public bool HasSupportForCentralizedPubSub
        {
            get { throw new NotImplementedException(); }
            protected set { throw new NotImplementedException(); }
        }
    }
}

#pragma warning disable 0067

namespace NServiceBus.Unicast.Transport
{
    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        Message = "No longer used, can safely be removed")]
    public class TransportReceiver
    {
    }
}

#pragma warning restore 0067

namespace NServiceBus
{
    public static partial class Headers
    {
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            Message = "The WinIdName header is no longer attached to outgoing message to avoid passing security related information on the wire. Should you rely on the header being present you can add a message mutator that sets it.")]
        public const string WindowsIdentityName = "WinIdName";
    }
}

namespace NServiceBus
{
    using System;

    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        Message = "Use IEndpointInstance to create sending session.")]
    public interface ISendOnlyBus : IDisposable
    {
    }

    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        Message = "IHandleMessages<T> now exposes the IMessageHandlerContext parameter. You can use this to access what used to be available in the IBus interface. Please use the provided context in extension points like message handlers or IEndpointInstance when outside the message processing pipeline.")]
    public interface IBus
    {
    }

    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        ReplacementTypeOrMember = "IStartableEndpoint")]
    public interface IStartableBus : IBus
    {
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "IStartableEndpoint")]
        IBus Start();
    }

    public static class Bus
    {
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "Endpoint.Create")]
        public static IStartableBus Create(EndpointConfiguration configuration)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "EndpointConfiguration.SendOnly")]
        public static IBus CreateSendOnly(EndpointConfiguration configuration)
        {
            throw new NotImplementedException();
        }
    }

    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        Message = "Use extension methods provided on ISendOnlyBus")]
    public class Schedule
    {
    }
}

namespace NServiceBus.Hosting.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public partial class AssemblyScanner
    {
        [ObsoleteEx(
            Message = "This method is no longer required since deep scanning of assemblies is done to detect an NServiceBus reference.",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public List<Assembly> MustReferenceAtLeastOneAssembly
        {
            get { throw new NotImplementedException(); }
        }
    }
}

namespace NServiceBus.Outbox
{
    using System;

    public partial class OutboxSettings
    {
        [ObsoleteEx(
            ReplacementTypeOrMember = "InMemoryOutboxSettingsExtensions.TimeToKeepDeduplicationData(TimeSpan time)",
            TreatAsErrorFromVersion = "6.0",
            RemoveInVersion = "7.0")]
        public void TimeToKeepDeduplicationData(TimeSpan time)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus
{
    using System;

    public partial class Saga
    {
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "RequestTimeout<TTimeoutMessageType>(IMessageHandlerContext, DateTime)")]
        protected void RequestTimeout<TTimeoutMessageType>(DateTime at)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Construct your message and pass it to the non Action overload.",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "RequestTimeout<TTimeoutMessageType>(IMessageHandlerContext DateTime, TTimeoutMessageType)")]
        protected void RequestTimeout<TTimeoutMessageType>(DateTime at, Action<TTimeoutMessageType> action)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "RequestTimeout<TTimeoutMessageType>(IMessageHandlerContext, DateTime, TTimeoutMessageType)")]
        protected void RequestTimeout<TTimeoutMessageType>(DateTime at, TTimeoutMessageType timeoutMessage)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "RequestTimeout<TTimeoutMessageType>(IMessageHandlerContext, TimeSpan)")]
        protected void RequestTimeout<TTimeoutMessageType>(TimeSpan within)
        {
            throw new NotImplementedException();
        }


        [ObsoleteEx(
            Message = "Construct your message and pass it to the non Action overload.",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "Saga.RequestTimeout<TTimeoutMessageType>(IMessageHandlerContext, TimeSpan, TTimeoutMessageType)")]
        protected void RequestTimeout<TTimeoutMessageType>(TimeSpan within, Action<TTimeoutMessageType> messageConstructor)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "RequestTimeout<TTimeoutMessageType>(IMessageHandlerContext, TimeSpan, TTimeoutMessageType)")]
        protected void RequestTimeout<TTimeoutMessageType>(TimeSpan within, TTimeoutMessageType timeoutMessage)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "ReplyToOriginator(IMessageHandlerContext, object)")]
        protected void ReplyToOriginator(object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Construct your message and pass it to the non Action overload.",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "ReplyToOriginator(IMessageHandlerContext, object)")]
        protected virtual void ReplyToOriginator<TMessage>(Action<TMessage> messageConstructor)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus
{
    using System;

    public static class IBusExtensions
    {
        [ObsoleteEx(
            Message = "Please use `IMessageHandlerContext.Reply(object message)` provided to message handlers instead.",
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7")]
        public static void Reply(this IBus bus, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Please use `IMessageHandlerContext.Reply<T>(Action<T> messageConstructor)` provided to message handlers instead.",
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7")]
        public static void Reply<T>(this IBus bus, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Please use `IMessageHandlerContext.SendLocal(object message)` provided to message handlers instead.",
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7")]
        public static void SendLocal(this IBus bus, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Please use `IMessageHandlerContext.SendLocal<T>(Action<T> messageConstructor)` provided to message handlers instead.",
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7")]
        public static void SendLocal<T>(this IBus bus, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Please use `IMessageHandlerContext.HandleCurrentMessageLater()` provided to message handlers instead.",
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7")]
        public static void HandleCurrentMessageLater(this IBus bus)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Please use `IMessageHandlerContext.ForwardCurrentMessageTo(string destination)` provided to message handlers instead.",
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7")]
        public static void ForwardCurrentMessageTo(this IBus bus, string destination)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Please use `IMessageHandlerContext.DoNotContinueDispatchingCurrentMessageToHandlers()` provided to message handlers instead.",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public static void DoNotContinueDispatchingCurrentMessageToHandlers(this IBus bus)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "Subscribe(Type messageType)")]
        public static void Subscribe(this IBus bus, Type messageType)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "Subscribe<T>()")]
        public static void Subscribe<T>(this IBus bus)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "Unsubscribe(Type messageType)")]
        public static void Unsubscribe(this IBus bus, Type messageType)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "Unsubscribe<T>()")]
        public static void Unsubscribe<T>(this IBus bus)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus
{
    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "config.UseTransport<MsmqTransport>().SubscriptionAuthorizer(Authorizer);")]
    public interface IAuthorizeSubscriptions
    {
    }
}

namespace NServiceBus.Settings
{
    using System;
    using System.Transactions;

    public class TransactionSettings
    {
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message =
                @"DoNotWrapHandlersExecutionInATransactionScope() has been removed since transaction scopes are no longer used by non DTC transports delay the dispatch of all outgoing operations until handlers have been executed.
In Version 6 handlers will only be wrapped in a transactionscope if running the MSMQ or SQLServer transports in default mode. This means that performing storage operations against data sources also supporting transaction scopes 
will escalate to a distributed transaction. Previous versions allowed opting out of this behavior using config.Transactions().DoNotWrapHandlersExecutionInATransactionScope(). In Version 6 it's recommended to use `EndpointConfiguration.UseTransport<MyTransport>().Transactions(TransportTransactionMode.ReceiveOnly)` to lean on native transport transaction and the new batched dispatch support to achieve the same level of consistency with better performance.
Suppressing the ambient transaction created by the MSMQ and SQL Server transports can still be achieved by creating a custom pipeline behavior with a suppressed transaction scope.")]
        public TransactionSettings DoNotWrapHandlersExecutionInATransactionScope()
        {
            throw new NotImplementedException();
        }


        [ObsoleteEx(
          RemoveInVersion = "7.0",
          TreatAsErrorFromVersion = "6.0",
          ReplacementTypeOrMember = "config.UseTransport<MyTransport>().Transactions(TransportTransactionMode.None);")]
        public TransactionSettings Disable()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "config.UseTransport<MyTransport>().Transactions(TransportTransactionMode.ReceiveOnly|TransportTransactionMode.SendsAtomicWithReceive);")]
        public TransactionSettings Enable()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "config.UseTransport<MyTransport>().Transactions(TransportTransactionMode.ReceiveOnly|TransportTransactionMode.SendsAtomicWithReceive);")]
        public TransactionSettings DisableDistributedTransactions()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "config.UseTransport<MyTransport>().Transactions(TransportTransactionMode.TransactionScope);")]
        public TransactionSettings EnableDistributedTransactions()
        {
            throw new NotImplementedException();
        }
        [ObsoleteEx(
                  RemoveInVersion = "7.0",
                  TreatAsErrorFromVersion = "6.0",
                  ReplacementTypeOrMember = "config.UnitOfWork().WrapHandlersInATransactionScope(isolationLevel: IsolationLevel.X);")]
        public TransactionSettings IsolationLevel(IsolationLevel isolationLevel)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
              RemoveInVersion = "7.0",
              TreatAsErrorFromVersion = "6.0",
              ReplacementTypeOrMember = "config.UnitOfWork().WrapHandlersInATransactionScope();")]
        public TransactionSettings WrapHandlersExecutionInATransactionScope()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
              RemoveInVersion = "7.0",
              TreatAsErrorFromVersion = "6.0",
               ReplacementTypeOrMember = "config.UnitOfWork().WrapHandlersInATransactionScope(timeout: TimeSpan.FromSeconds(X));")]
        public TransactionSettings DefaultTimeout(TimeSpan defaultTimeout)
        {
            throw new NotImplementedException();
        }
    }

    namespace NServiceBus
    {
        [ObsoleteEx(
             RemoveInVersion = "7.0",
             TreatAsErrorFromVersion = "6.0",
              Message = "No longer used, can safely be removed")]
        public static class TransactionSettingsExtentions
        {
            [ObsoleteEx(
               RemoveInVersion = "7.0",
               TreatAsErrorFromVersion = "6.0",
                Message = "No longer used, can safely be removed")]
            public static TransactionSettings Transactions(this EndpointConfiguration config)
            {
                throw new NotImplementedException();
            }
        }
    }
}

namespace NServiceBus
{
    using System;

    public static partial class SerializationConfigExtensions
    {

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "To use a custom serializer derive from SerializationDefinition and provide a factory method for creating the serializer instance.")]
        public static void UseSerialization(this EndpointConfiguration config, Type serializerType)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Serialization
{
    [ObsoleteEx(
           RemoveInVersion = "7.0",
           TreatAsErrorFromVersion = "6.0",
           Message = "To use a custom serializer derive from SerializationDefinition and provide a factory method for creating the serializer instance.")]
    public abstract class ConfigureSerialization
    {
    }
}

namespace NServiceBus.Serializers.Json
{
    [ObsoleteEx(
           RemoveInVersion = "7.0",
           TreatAsErrorFromVersion = "6.0",
           Message = "Built-in serializers are internal. Please consider switching to alternative (e.g. Json.net) or copy the serializer code.")]
    public class JsonMessageSerializer
    {
    }

}

namespace NServiceBus.Serializers.XML
{
    [ObsoleteEx(
           RemoveInVersion = "7.0",
           TreatAsErrorFromVersion = "6.0",
           Message = "Built-in serializers are internal. Please consider switching to alternative (e.g. XmlSerializer) or copy the serializer code.")]
    public class XmlMessageSerializer
    {
    }
}

namespace NServiceBus.Transports.Msmq
{

    [ObsoleteEx(
           RemoveInVersion = "7.0",
           TreatAsErrorFromVersion = "6.0",
           Message = "No longer available, please see our documentation for native sends for alternative solutions.")]
    public class MsmqMessageSender
    {
    }
}

namespace NServiceBus.Transports.Msmq.Config
{
    [ObsoleteEx(
         RemoveInVersion = "7.0",
         TreatAsErrorFromVersion = "6.0",
         Message = "No longer available, please see our documentation for native sends for alternative solutions.")]
    public class MsmqSettings
    {
    }
}