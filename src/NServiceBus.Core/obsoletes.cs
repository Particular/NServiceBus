// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedParameter.Local


using NServiceBus;

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
    using System.Text;
    using DataBus;
    using ObjectBuilder;

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
        Message = "Use `IMessageHandlerContext` provided to message handlers instead.",
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
            Message = "This is no longer supported. If you want full control over what happens when a message fails (including retries) override the MoveFaultsToErrorQueue behavior.",
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
        ReplacementTypeOrMember = "endpointConfiguration.Recoverability().Delayed(delayed => )")]
    public static class SecondLevelRetriesConfigExtensions
    {
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "endpointConfiguration.Recoverability().Delayed(delayed => )")]
        public static SecondLevelRetriesSettings SecondLevelRetries(this EndpointConfiguration config)
        {
            throw new NotImplementedException();
        }
    }

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "endpointConfiguration.Recoverability().CustomPolicy(Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> @custom)")]
    public class SecondLevelRetriesSettings
    {
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "endpointConfiguration.Recoverability().CustomPolicy(Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> @custom)")]
        public void CustomRetryPolicy(Func<TransportMessage, TimeSpan> customPolicy)
        {
            throw new NotImplementedException();
        }
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
    public class BusConfiguration
    {
    }

    public partial class EndpointConfiguration
    {
        [ObsoleteEx(
            ReplacementTypeOrMember = "EndpointConfiguration.AddHeaderToAllOutgoingMessages(string key,string value)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public IDictionary<string, string> OutgoingHeaders
        {
            get { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "EndpointConfiguration.ExcludeTypes")]
        public void TypesToScan(IEnumerable<Type> typesToScan)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "EndpointConfiguration.ExcludeAssemblies")]
        public void AssembliesToScan(IEnumerable<Assembly> assemblies)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "EndpointConfiguration.ExcludeAssemblies")]
        public void AssembliesToScan(params Assembly[] assemblies)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "EndpointConfiguration.ExcludeAssemblies")]
        public void ScanAssembliesInDirectory(string probeDirectory)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            ReplacementTypeOrMember = "EndpointConfiguration.OverridePublicReturnAddress(string address)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public void OverridePublicReturnAddress(Address address)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Endpoint name is now a mandatory constructor argument on EndpointConfiguration.",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public void EndpointName(string name)
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

    [ObsoleteEx(
        RemoveInVersion = "7",
        TreatAsErrorFromVersion = "6",
        ReplacementTypeOrMember = "Notifications")]
    public class BusNotifications
    {
    }

    public partial class Notifications
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
        [ObsoleteEx(
            ReplacementTypeOrMember = "RegisterEncryptionService(this EndpointConfiguration config, Func<IEncryptionService> func)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "It is no longer possible to access the builder to create an encryption service. If container access is required use the container directly in the factory.")]
        public static void RegisterEncryptionService(this EndpointConfiguration config, Func<IBuilder, IEncryptionService> func)
        {
            throw new NotImplementedException();
        }
    }

    [ObsoleteEx(
        Message = "`IWantToRunWhenBusStartsAndStops` has been moved to the host implementations and renamed. If you're self-hosting, instead of using this interface, you can call any startup code right before `Endpoint.Create` or any cleanup code right after `Endpoint.Stop`. When using either NServiceBus.Host or NServiceBus.Host.AzureCloudService, use the host's interface `IWantToRunWhenEndpointStartsAndStops` instead.",
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0")]
    public interface IWantToRunWhenBusStartsAndStops
    {
    }

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "PersistenceExtensions<T, S>")]
    public class PersistenceExtentions<T, S>
    {
    }

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "PersistenceExtensions<T>")]
    public class PersistenceExtentions<T>
    {
    }

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "PersistenceExtensions")]
    public class PersistenceExtentions
    {
    }

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "SerializationExtensions<T>")]
    public class SerializationExtentions<T>
    {
    }

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0")]
    public static class ScaleOutExtentions
    {
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public static Settings.ScaleOutSettings ScaleOut(this EndpointConfiguration config)
        {
            throw new NotImplementedException();
        }
    }

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "SettingsExtensions")]
    public static class SettingsExtentions
    {
    }

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "LoadMessageHandlersExtensions")]
    public static class LoadMessageHandlersExtentions
    {
    }

    public static partial class ConfigureFileShareDataBus
    {
        [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "BasePath(this DataBusExtensions<FileShareDataBus> config, string basePath)")]
        public static DataBusExtentions<FileShareDataBus> BasePath(this DataBusExtentions<FileShareDataBus> config, string basePath)
        {
            throw new NotImplementedException();
        }
    }

    public static partial class JsonSerializerConfigurationExtensions
    {
        [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "Encoding(this SerializationExtensions<JsonSerializer> config, Encoding encoding)")]
        public static void Encoding(this SerializationExtentions<JsonSerializer> config, Encoding encoding)
        {
            throw new NotImplementedException();
        }
    }

    public static partial class XmlSerializationExtensions
    {
        [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "DontWrapRawXml(this SerializationExtensions<XmlSerializer> config)")]
        public static SerializationExtentions<XmlSerializer> DontWrapRawXml(this SerializationExtentions<XmlSerializer> config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "Namespace(this SerializationExtensions<XmlSerializer> config, string namespaceToUse)")]
        public static SerializationExtentions<XmlSerializer> Namespace(this SerializationExtentions<XmlSerializer> config, string namespaceToUse)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "SanitizeInput(this SerializationExtensions<XmlSerializer> config)")]
        public static SerializationExtentions<XmlSerializer> SanitizeInput(this SerializationExtentions<XmlSerializer> config)
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
    using Unicast;

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
        Message = "Have the mutator implement both IMutateOutgoingMessages and IMutateIncomingMessages ",
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0")]
    public interface IMessageMutator
    {
    }

    [ObsoleteEx(
        Message = "Have the mutator implement both IMutateIncomingTransportMessages and IMutateOutgoingTransportMessages",
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
            Message = @"Use the overload of the Send, Publish or Reply method that accepts an options parameter. Call options.SetHeader(""MyHeader"",""MyValue"") instead.")]
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

    [ObsoleteEx(
        Message = "Use extensions provided by the TransportDefinition class instead",
        TreatAsErrorFromVersion = "6.0",
        RemoveInVersion = "7.0")]
    public class ConfigureTransport
    {
    }

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "Encryption is no longer enabled by default. Encryption gets enabled by calling configuration.RegisterEncryptionService or configuration.RijndaelEncryptionService.")]
    public class Encryptor { }
}

namespace NServiceBus.Transports
{
    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "NServiceBus.Transport.IDispatchMessages")]
    public interface IDeferMessages
    {
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "NServiceBus.Transport.ICancelDeferredMessages")]
        void ClearDeferredMessages(string headerKey, string headerValue);
    }
}

namespace NServiceBus.Transports
{
    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "NServiceBus.Transport.IDispatchMessages")]
    public interface IPublishMessages
    {
    }
}

namespace NServiceBus.Transports
{
    using Unicast;

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "NServiceBus.Transport.IDispatchMessages")]
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

namespace NServiceBus.Transport
{
    public partial class TransportInfrastructure
    {
        [ObsoleteEx(
            RemoveInVersion = "8.0",
            TreatAsErrorFromVersion = "7.0",
            Message = "The outbox consent is no longer required. It is safe to ignore this property.")]
        public bool RequireOutboxConsent { get; protected set; }
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
    using System;

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
        public bool SubscribeToPlainMessages
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
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
    using System;
    using System.Configuration;

    [ObsoleteEx(
        Message = "Use the feature concept instead via A class which inherits from `NServiceBus.Features.Feature` and use `configuration.EnableFeature<YourClass>()`",
        RemoveInVersion = "7",
        TreatAsErrorFromVersion = "6")]
    public interface IWantToRunWhenConfigurationIsComplete
    {
    }

    [ObsoleteEx(
        Message = Error,
        RemoveInVersion = "7",
        TreatAsErrorFromVersion = "6")]
    public class SecondLevelRetriesConfig : ConfigurationSection
    {
        const string Error = "Second Level Retries has been renamed to Delayed Retries. The app.config API has been removed, use the code API via endpointConfiguration.Recoverability().Delayed(settings => ...);.";

        public SecondLevelRetriesConfig()
        {
            Properties.Add(new ConfigurationProperty("Enabled", typeof(bool), true));
            Properties.Add(new ConfigurationProperty("TimeIncrease", typeof(TimeSpan), Recoverability.DefaultTimeIncrease, null, new TimeSpanValidator(TimeSpan.Zero, TimeSpan.MaxValue), ConfigurationPropertyOptions.None));
            Properties.Add(new ConfigurationProperty("NumberOfRetries", typeof(int), Recoverability.DefaultNumberOfRetries, null, new IntegerValidator(0, int.MaxValue), ConfigurationPropertyOptions.None));
        }

        [ObsoleteEx(
            Message = Error + " To disable use endpointConfiguration.Recoverability().Delayed(settings => settings.NumberOfRetries(0));",
            RemoveInVersion = "7",
            TreatAsErrorFromVersion = "6")]
        public bool Enabled
        {
            get { return (bool) this["Enabled"]; }
            set { this["Enabled"] = value; }
        }

        [ObsoleteEx(
            Message = Error + " To change the TimeIncrease use endpointConfiguration.Recoverability().Delayed(settings => settings.TimeIncrease(TimeSpan.FromMinutes(5));",
            RemoveInVersion = "7",
            TreatAsErrorFromVersion = "6")]
        public TimeSpan TimeIncrease
        {
            get { return (TimeSpan) this["TimeIncrease"]; }
            set { this["TimeIncrease"] = value; }
        }

        [ObsoleteEx(
            Message = Error + " To change the NumberOfRetries use endpointConfiguration.Recoverability().Delayed(settings => settings.NumberOfRetries(5);",
            RemoveInVersion = "7",
            TreatAsErrorFromVersion = "6")]
        public int NumberOfRetries
        {
            get { return (int) this["NumberOfRetries"]; }
            set { this["NumberOfRetries"] = value; }
        }
    }

    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        ReplacementTypeOrMember = "EndpointConfiguration.EnlistWithLegacyMSMQDistributor")]
    public class MasterNodeConfig : ConfigurationSection
    {
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "EndpointConfiguration.EnlistWithLegacyMSMQDistributor")]
        [ConfigurationProperty("Node", IsRequired = false)]
        public string Node { get; set; }
    }

    [ObsoleteEx(
        Message = Error,
        RemoveInVersion = "7",
        TreatAsErrorFromVersion = "6")]
    public class TransportConfig : ConfigurationSection
    {
        const string Error = "The app.config API TransportConfig has been removed, use the code API.";

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            Message = Error + " To change the concurrency level use endpointConfiguration.LimitMessageProcessingConcurrencyTo(1);")]
        [ConfigurationPropertyAttribute("MaximumConcurrencyLevel", DefaultValue = 0, IsRequired = false)]
        public int MaximumConcurrencyLevel
        {
            get { return (int) this["MaximumConcurrencyLevel"]; }
            set { this["MaximumConcurrencyLevel"] = value; }
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            Message = Error + " To change the NumberOfRetries use endpointConfiguration.Recoverability().Immediate(settings => settings.NumberOfRetries(5);")]
        [ConfigurationPropertyAttribute("MaxRetries", DefaultValue = 5, IsRequired = false)]
        public int MaxRetries
        {
            get { return (int) this["MaxRetries"]; }
            set { this["MaxRetries"] = value; }
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            Message = "Message throughput throttling has been removed. Consult the documentation for further information.")]
        [ConfigurationPropertyAttribute("MaximumMessageThroughputPerSecond", DefaultValue = -1, IsRequired = false)]
        public int MaximumMessageThroughputPerSecond
        {
            get { return (int) this["MaximumMessageThroughputPerSecond"]; }
            set { this["MaximumMessageThroughputPerSecond"] = value; }
        }
    }

    public partial class UnicastBusConfig
    {
        [ConfigurationProperty("DistributorControlAddress", IsRequired = false)]
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            Message = "Switch to the code API by using 'EndpointConfiguration.EnlistWithLegacyMSMQDistributor' instead.")]
        public string DistributorControlAddress
        {
            get
            {
                var result = this["DistributorControlAddress"] as string;
                if (string.IsNullOrWhiteSpace(result))
                {
                    result = null;
                }

                return result;
            }
            set { this["DistributorControlAddress"] = value; }
        }

        [ConfigurationProperty("DistributorDataAddress", IsRequired = false)]
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            Message = "Switch to the code API by using 'EndpointConfiguration.EnlistWithLegacyMSMQDistributor' instead.")]
        public string DistributorDataAddress
        {
            get
            {
                var result = this["DistributorDataAddress"] as string;
                if (string.IsNullOrWhiteSpace(result))
                {
                    result = null;
                }

                return result;
            }
            set { this["DistributorDataAddress"] = value; }
        }

        [ConfigurationProperty("ForwardReceivedMessagesTo", IsRequired = false)]
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            Message = "Use 'EndpointConfiguration.ForwardReceivedMessagesTo' to configure the forwarding address.")]
        public string ForwardReceivedMessagesTo
        {
            get
            {
                var result = this["ForwardReceivedMessagesTo"] as string;
                if (string.IsNullOrWhiteSpace(result))
                {
                    result = null;
                }

                return result;
            }
            set { this["ForwardReceivedMessagesTo"] = value; }
        }
    }
}

namespace NServiceBus.SecondLevelRetries.Config
{
    using System;

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "NServiceBus.SecondLevelRetriesSettings")]
    public class SecondLevelRetriesSettings
    {
        /// <summary>
        /// Register a custom retry policy.
        /// </summary>
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "NServiceBus.SecondLevelRetriesSettings.CustomRetryPolicy(Func<IncomingMessage, TimeSpan> customPolicy)")]
        public void CustomRetryPolicy(Func<TransportMessage, TimeSpan> customPolicy)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Faults
{
    using System;

    public partial class ErrorsNotifications
    {
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6.0",
            RemoveInVersion = "7.0",
            ReplacementTypeOrMember = nameof(MessageHasBeenSentToDelayedRetries)
            )]
        public EventHandler MessageHasBeenSentToSecondLevelRetries;

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6.0",
            RemoveInVersion = "7.0",
            ReplacementTypeOrMember = nameof(MessageHasFailedAnImmediateRetryAttempt))]
        public EventHandler MessageHasFailedAFirstLevelRetryAttempt;
    }

    [ObsoleteEx(
         Message = "First Level Retries has been renamed to Immediate Retries",
         RemoveInVersion = "7",
         TreatAsErrorFromVersion = "6",
         ReplacementTypeOrMember = "NServiceBus.Faults.ImmediateRetryMessage")]
    public struct FirstLevelRetry
    {
    }
    [ObsoleteEx(
         Message = "Second Level Retries has been renamed to Delayed Retries",
         RemoveInVersion = "7",
         TreatAsErrorFromVersion = "6",
         ReplacementTypeOrMember = "NServiceBus.Faults.DelayedRetryMessage")]
    public struct SecondLevelRetry
    {
    }

    [ObsoleteEx(
         Message = "IManageMessageFailures is no longer an extension point. To take control of the error handling part of the message processing pipeline, review the Version 5 to 6 upgrade guide for details.",
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
        Message = "Transaction settings is no longer available via this class. See obsoletes on individual members for further details",
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0")]
    public class TransactionSettings
    {
        [ObsoleteEx(
            Message = "No longer used",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public TransactionSettings(bool isTransactional, TimeSpan transactionTimeout, IsolationLevel isolationLevel, bool suppressDistributedTransactions, bool doNotWrapHandlersExecutionInATransactionScope)
        {
        }

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
            Message = "DoNotWrapHandlersExecutionInATransactionScope is no longer used here. Use settings.GetOrDefault<bool>('Transactions.DoNotWrapHandlersExecutionInATransactionScope') instead",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public bool DoNotWrapHandlersExecutionInATransactionScope { get; set; }

        [ObsoleteEx(
            Message = "SuppressDistributedTransactions is no longer used here. Use `context.Settings.GetRequiredTransactionModeForReceives() != Transactions.TransactionScope` instead.",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public bool SuppressDistributedTransactions { get; set; }

        [ObsoleteEx(
            Message = "IsTransactional is no longer used here. Use `context.Settings.GetRequiredTransactionModeForReceives() != Transactions.None` instead.",
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

    [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7")]
    public class ScaleOutSettings
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
            ReplacementTypeOrMember = "EndpointConfiguration.MakeInstanceUniquelyAddressable(string discriminator)")]
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
        Message = "No longer used, use the new callbacks api described in the version 6 upgrade guide")]
    public class BusAsyncResultEventArgs
    {
    }
}

namespace NServiceBus.Unicast
{
    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        Message = "No longer used, use the new callbacks api described in the version 6 upgrade guide")]
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
    using System;

    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        ReplacementTypeOrMember = "Behavior<T>")]
    public interface IBehavior<in TContext>
    {
    }

    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        Message = "The pipeline context is no longer avaliable via dependency injection. Use a custom behavior as described in the version 6 upgrade guide")]
    public class PipelineExecutor
    {
    }

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "WellKnownSteps are obsolete. Use an appropriate pipeline stage for your behavior instead. Consult the pipeline extension documentation for more information.")]
    public class WellKnownStep
    {
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use an appropriate pipeline stage for your behavior instead. Consult the pipeline extension documentation for more information.")]
        public static WellKnownStep HostInformation;

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use an appropriate pipeline stage for your behavior instead. Consult the pipeline extension documentation for more information.")]
        public static WellKnownStep ProcessingStatistics;

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use an appropriate pipeline stage for your behavior instead. Consult the pipeline extension documentation for more information.")]
        public static readonly WellKnownStep AuditProcessedMessage;

        [ObsoleteEx(
            Message = "The child container creation is now an integral part of the pipeline invocation and no longer a separate behavior.",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public static readonly WellKnownStep CreateChildContainer;

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use an appropriate pipeline stage for your behavior instead. Consult the pipeline extension documentation for more information.")]
        public static readonly WellKnownStep ExecuteUnitOfWork;

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use an appropriate pipeline stage for your behavior instead. Consult the pipeline extension documentation for more information.")]
        public static readonly WellKnownStep MutateIncomingTransportMessage;

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use an appropriate pipeline stage for your behavior instead. Consult the pipeline extension documentation for more information.")]
        public static readonly WellKnownStep DispatchMessageToTransport;

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use an appropriate pipeline stage for your behavior instead. Consult the pipeline extension documentation for more information.")]
        public static readonly WellKnownStep InvokeHandlers;

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use an appropriate pipeline stage for your behavior instead. Consult the pipeline extension documentation for more information.")]
        public static readonly WellKnownStep MutateIncomingMessages;

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use an appropriate pipeline stage for your behavior instead. Consult the pipeline extension documentation for more information.")]
        public static readonly WellKnownStep InvokeSaga;

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use an appropriate pipeline stage for your behavior instead. Consult the pipeline extension documentation for more information.")]
        public static readonly WellKnownStep MutateOutgoingMessages;

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use an appropriate pipeline stage for your behavior instead. Consult the pipeline extension documentation for more information.")]
        public static readonly WellKnownStep MutateOutgoingTransportMessage;

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use an appropriate pipeline stage for your behavior instead. Consult the pipeline extension documentation for more information.")]
        public static readonly WellKnownStep EnforceSendBestPractices;

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use an appropriate pipeline stage for your behavior instead. Consult the pipeline extension documentation for more information.")]
        public static readonly WellKnownStep EnforceReplyBestPractices;

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use an appropriate pipeline stage for your behavior instead. Consult the pipeline extension documentation for more information.")]
        public static readonly WellKnownStep EnforcePublishBestPractices;

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use an appropriate pipeline stage for your behavior instead. Consult the pipeline extension documentation for more information.")]
        public static readonly WellKnownStep EnforceSubscribeBestPractices;

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use an appropriate pipeline stage for your behavior instead. Consult the pipeline extension documentation for more information.")]
        public static readonly WellKnownStep EnforceUnsubscribeBestPractices;
    }

    public abstract partial class RegisterStep
    {
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use an appropriate pipeline stage for your behavior instead. Consult the pipeline extension documentation for more information.")]
        public void InsertBeforeIfExists(WellKnownStep step)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use an appropriate pipeline stage for your behavior instead. Consult the pipeline extension documentation for more information.")]
        public void InsertBefore(WellKnownStep step)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use an appropriate pipeline stage for your behavior instead. Consult the pipeline extension documentation for more information.")]
        public void InsertAfterIfExists(WellKnownStep step)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Use an appropriate pipeline stage for your behavior instead. Consult the pipeline extension documentation for more information.")]
        public void InsertAfter(WellKnownStep step)
        {
            throw new NotImplementedException();
        }
    }

    public partial class PipelineSettings
    {
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "Remove(string stepId)")]
        public void Remove(WellKnownStep wellKnownStep)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "Replace(string stepId, Type newBehavior, string description)")]
        public void Replace(WellKnownStep wellKnownStep, Type newBehavior, string description = null)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Satellites
{
    [ObsoleteEx(
        Message = "No longer an extension point. Instead create a Feature and use FeatureConfigurationContext.AddSatelliteReceiver(...).",
        RemoveInVersion = "7",
        TreatAsErrorFromVersion = "6")]
    public interface IAdvancedSatellite
    {
    }

    [ObsoleteEx(
        Message = "No longer an extension point. Instead create a Feature and use FeatureConfigurationContext.AddSatelliteReceiver(...).",
        RemoveInVersion = "7",
        TreatAsErrorFromVersion = "6")]
    public interface ISatellite
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
    public static class ControlMessage
    {
    }

    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        ReplacementTypeOrMember = "NServiceBus.Transport.IPushMessages")]
    public interface ITransport
    {
    }

    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        Message = "No longer used, can safely be removed")]
    public class TransportMessageReceivedEventArgs
    {
    }

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
    [ObsoleteEx(
        TreatAsErrorFromVersion = "6",
        RemoveInVersion = "7",
        Message = "The namespace NServiceBus.Transports was renamed to NServiceBus.Transport.",
        ReplacementTypeOrMember = "NServiceBus.Transport.TransportDefinition")]
    public abstract class TransportDefinition
    {
    }
}

namespace NServiceBus.Transport
{
    using System;

    public abstract partial class TransportDefinition
    {
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            Message = "Use TransportInfrastructure.TransactionMode == TransportTransactionMode.TransactionScope instead.")]
        public bool? HasSupportForDistributedTransactions
        {
            get { throw new NotImplementedException(); }
            protected set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            Message = "Use TransportInfrastructure.TransactionMode == TransportTransactionMode.SendsAtomicWithReceive instead.")]
        public bool HasSupportForMultiQueueNativeTransactions
        {
            get { throw new NotImplementedException(); }
            protected set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            Message = "Use TransportInfrastructure.OutboundRoutingPolicy.Publishes == OutboundRoutingType.Multicast instead.")]
        public bool HasNativePubSubSupport
        {
            get { throw new NotImplementedException(); }
            protected set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            Message = "The concept of centralized publish and subscribe is no longer available.")]
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
        Message = "IHandleMessages<T> now exposes the IMessageHandlerContext parameter. Use this to access what used to be available in the IBus interface. Use the provided context in extension points like message handlers or IEndpointInstance when outside the message processing pipeline.")]
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
            Message = "Sagas no longer provide access to bus operations via the .Bus property. Use the context parameter on the Handle method.",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public IBus Bus
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "RequestTimeout<TTimeoutMessageType>(IMessageHandlerContext, DateTime)")]
        protected void RequestTimeout<TTimeoutMessageType>(DateTime at)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Construct the message and pass it to the non Action overload.",
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
            Message = "Construct the message and pass it to the non Action overload.",
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
            Message = "Construct the message and pass it to the non Action overload.",
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
            Message = "Use `IMessageHandlerContext.Reply(object message)` provided to message handlers instead.",
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7")]
        public static void Reply(this IBus bus, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use `IMessageHandlerContext.Reply<T>(Action<T> messageConstructor)` provided to message handlers instead.",
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7")]
        public static void Reply<T>(this IBus bus, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use `IMessageHandlerContext.SendLocal(object message)` provided to message handlers instead.",
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7")]
        public static void SendLocal(this IBus bus, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use `IMessageHandlerContext.SendLocal<T>(Action<T> messageConstructor)` provided to message handlers instead.",
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7")]
        public static void SendLocal<T>(this IBus bus, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use `IMessageHandlerContext.HandleCurrentMessageLater()` provided to message handlers instead.",
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7")]
        public static void HandleCurrentMessageLater(this IBus bus)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use `IMessageHandlerContext.ForwardCurrentMessageTo(string destination)` provided to message handlers instead.",
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7")]
        public static void ForwardCurrentMessageTo(this IBus bus, string destination)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use `IMessageHandlerContext.DoNotContinueDispatchingCurrentMessageToHandlers()` provided to message handlers instead.",
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
        Message = "Built-in serializers are internal. Switch to an alternative (e.g. Json.net) or copy the serializer code.")]
    public class JsonMessageSerializer
    {
    }
}

namespace NServiceBus.Serializers.XML
{
    //[ObsoleteEx(
    //    RemoveInVersion = "7.0",
    //    TreatAsErrorFromVersion = "6.0",
    //    Message = "Built-in serializers are internal. Switch to an  alternative (e.g. XmlSerializer) or copy the serializer code.")]
    //public class XmlMessageSerializer
    //{
    //}
}

namespace NServiceBus.Transports.Msmq
{
    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "No longer available, see the documentation for native sends for alternative solutions.")]
    public class MsmqMessageSender
    {
    }
}

namespace NServiceBus.Transports.Msmq.Config
{
    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "No longer available, see the documentation for native sends for alternative solutions.")]
    public class MsmqSettings
    {
    }
}

public static class ConfigureHandlerSettings
{
    [ObsoleteEx(
         RemoveInVersion = "7.0",
         TreatAsErrorFromVersion = "6.0",
         Message = "Setting property values explicitly is no longer supported via this API. Use `.ConfigureComponent(b=> new MyMessageHandler(){ MyProperty = X})` to get full control over handler creation.")]
    public static void InitializeHandlerProperty<THandler>(this EndpointConfiguration config, string property, object value)
    {
    }
}


namespace NServiceBus.ObjectBuilder
{
    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "Setting property values explicitly is no longer supported via this API. Use `.ConfigureComponent(b=> new MyMessageHandler(){ MyProperty = X})` to get full control over handler creation.")]
    public interface IComponentConfig
    {
    }

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "Setting property values explicitly is no longer supported via this API. Use `.ConfigureComponent(b=> new MyMessageHandler(){ MyProperty = X})` to get full control over handler creation.")]
    public interface IComponentConfig<T>
    {
    }
}

namespace NServiceBus.ObjectBuilder
{
    using System;
    using System.Linq.Expressions;

    [ObsoleteEx(
      RemoveInVersion = "7.0",
      TreatAsErrorFromVersion = "6.0",
      Message = "Setting property values explicitly is no longer supported via this API. Use `.ConfigureComponent(b=> new MyMessageHandler(){ MyProperty = X})` to get full control over handler creation.")]
    public static class IConfigureComponentObsoleteExtensions
    {
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Setting property values explicitly is no longer supported via this API. Use `.ConfigureComponent(b=> new MyMessageHandler(){ MyProperty = X})` to get full control over handler creation.")]
        public static IConfigureComponents ConfigureProperty<T>(this IConfigureComponents config, Expression<Func<T, object>> property, object value)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            Message = "Setting property values explicitly is no longer supported via this API. Use `.ConfigureComponent(b=> new MyMessageHandler(){ MyProperty = X})` to get full control over handler creation.")]
        public static IConfigureComponents ConfigureProperty<T>(this IConfigureComponents config, string propertyName, object value)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Settings
{
    using System;
    using System.Linq.Expressions;
    using ObjectBuilder;

    public partial class SettingsHolder
    {
        [ObsoleteEx(
           RemoveInVersion = "7.0",
           TreatAsErrorFromVersion = "6.0",
           Message = "Setting property values explicitly is no longer supported via this API. Use `.ConfigureComponent(b=> new MyMessageHandler(){ MyProperty = X})` to get full control over handler creation.")]
        public void ApplyTo<T>(IComponentConfig config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
          RemoveInVersion = "7.0",
          TreatAsErrorFromVersion = "6.0",
          Message = "Setting property values explicitly is no longer supported via this API. Use `.ConfigureComponent(b=> new MyMessageHandler(){ MyProperty = X})` to get full control over handler creation.")]
        public void ApplyTo(Type componentType, IComponentConfig config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
          RemoveInVersion = "7.0",
          TreatAsErrorFromVersion = "6.0",
          ReplacementTypeOrMember = "Set(string key, object value)")]
        public void SetProperty<T>(Expression<Func<T, object>> property, object value)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
          RemoveInVersion = "7.0",
          TreatAsErrorFromVersion = "6.0",
          ReplacementTypeOrMember = "Set(string key, object value)")]
        public void SetPropertyDefault<T>(Expression<Func<T, object>> property, object value)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Settings
{
    using System;
    using ObjectBuilder;

    public static class ReadOnlySettingsExtensions
    {
        [ObsoleteEx(
                RemoveInVersion = "7.0",
                TreatAsErrorFromVersion = "6.0",
                Message = "Setting property values explicitly is no longer supported via this API. Use `.ConfigureComponent(b=> new MyMessageHandler(){ MyProperty = X})` to get full control over handler creation.")]
        public static void ApplyTo<T>(IComponentConfig config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
              RemoveInVersion = "7.0",
              TreatAsErrorFromVersion = "6.0",
              Message = "Setting property values explicitly is no longer supported via this API. Use `.ConfigureComponent(b=> new MyMessageHandler(){ MyProperty = X})` to get full control over handler creation.")]
        public static void ApplyTo(Type componentType, IComponentConfig config)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.DataBus
{
    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "DataBusExtensions<T>")]
    public class DataBusExtentions<T>
    {
    }

    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        ReplacementTypeOrMember = "DataBusExtensions")]
    public class DataBusExtentions
    {
    }
}

namespace NServiceBus.Features
{
    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "FirstLevelRetries is no longer a separate feature. Please use endpointConfiguration.Recoverability().Immediate(cfg => cfg.NumberOfRetries(0)); to disable Immediate Retries.")]
    public class FirstLevelRetries : Feature
    {
        internal FirstLevelRetries()
        {
        }
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}

namespace NServiceBus.Features
{
    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "SecondLevelRetries is no longer a separate feature. Please use endpointConfiguration.Recoverability().Delayed(cfg => cfg.NumberOfRetries(0)) to disable Delayed Retries.")]
    public class SecondLevelRetries : Feature
    {
        internal SecondLevelRetries()
        {
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
        }
    }
}




