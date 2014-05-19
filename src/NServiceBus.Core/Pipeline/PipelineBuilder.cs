namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using Audit;
    using Contexts;
    using DataBus;
    using MessageMutator;
    using NServiceBus.MessageMutator;
    using Sagas;
    using Settings;
    using Unicast.Behaviors;
    using Unicast.Messages;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using UnitOfWork;

    class PipelineBuilder
    {
        public PipelineBuilder()
        {
            var removals = SettingsHolder.Instance.Get<List<RemoveBehavior>>("Pipeline.Removals");
            var replacements = SettingsHolder.Instance.Get<List<ReplaceBehavior>>("Pipeline.Replacements");

            coordinator = new BehaviorRegistrationsCoordinator(removals, replacements);

            RegisterIncomingCoreBehaviors();
            RegisterOutgoingCoreBehaviors();
            RegisterAdditionalBehaviors();

            var model = coordinator.BuildRuntimeModel();
            Incoming = new List<RegisterBehavior>();
            Outgoing = new List<RegisterBehavior>();
            var behaviorType = typeof(IBehavior<>);
            var outgoingContextType = typeof(OutgoingContext);
            var incomingContextType = typeof(IncomingContext);

            foreach (var rego in model)
            {
                if (behaviorType.MakeGenericType(incomingContextType).IsAssignableFrom(rego.BehaviorType))
                {
                    Incoming.Add(rego);
                }

                if (behaviorType.MakeGenericType(outgoingContextType).IsAssignableFrom(rego.BehaviorType))
                {
                    Outgoing.Add(rego);
                }
            }
        }

        public List<RegisterBehavior> Incoming { get; private set; }
        public List<RegisterBehavior> Outgoing { get; private set; }

        void RegisterAdditionalBehaviors()
        {
            var additions = SettingsHolder.Instance.Get<List<RegisterBehavior>>("Pipeline.Additions");

            foreach (var rego in additions)
            {
                coordinator.Register(rego);
            }
        }

        void RegisterIncomingCoreBehaviors()
        {
            coordinator.Register(WellKnownBehavior.ChildContainer, typeof(ChildContainerBehavior), "Creates the child container");
            coordinator.Register("MessageReceivedLogging", typeof(MessageHandlingLoggingBehavior), "Logs the message received");
            coordinator.Register(WellKnownBehavior.AuditForwarder, typeof(AuditBehavior), "Forward message to audit queue after message is successfully processed");
            coordinator.Register("ForwardMessageTo", typeof(ForwardBehavior), "Forwards message to");
            coordinator.Register("SubscriptionReceiverBehavior", typeof(SubscriptionReceiverBehavior), "Check for subscription messages");
            coordinator.Register(WellKnownBehavior.UnitOfWork, typeof(UnitOfWorkBehavior), "Executes the UoW");
            coordinator.Register(WellKnownBehavior.MutateIncomingTransportMessage, typeof(ApplyIncomingTransportMessageMutatorsBehavior), "Executes IMutateIncomingTransportMessages");
            coordinator.Register("RemoveHeaders", typeof(RemoveIncomingHeadersBehavior), "For backward compatibility we need to remove some headers from the incoming message");
            coordinator.Register("CallBack", typeof(CallbackInvocationBehavior), "Updates the callback inmemory dictionary");
            coordinator.Register(WellKnownBehavior.ExtractLogicalMessages, typeof(ExtractLogicalMessagesBehavior), "It splits the raw message into multiple logical messages");
            coordinator.Register(WellKnownBehavior.ExecuteLogicalMessages, typeof(ExecuteLogicalMessagesBehavior), "Starts the execution of each logical message");
            coordinator.Register(WellKnownBehavior.MutateIncomingMessages, typeof(ApplyIncomingMessageMutatorsBehavior), "Executes IMutateIncomingMessages");
            coordinator.Register("DataBusReceive", typeof(DataBusReceiveBehavior), "Copies the databus shared data back to the logical message"); //todo: we'll make this optional as soon as we have a way to manipulate the pipeline
            coordinator.Register(WellKnownBehavior.ExecuteHandlers, typeof(LoadHandlersBehavior), "Executes all IHandleMessages<T>");
            coordinator.Register("SetCurrentMessageBeingHandled", typeof(SetCurrentMessageBeingHandledBehavior), "Sets the static current message (this is used by the headers)");
            coordinator.Register("AuditInvokedSaga", typeof(AuditInvokedSagaBehavior), "Populates the InvokedSaga header");
            coordinator.Register(WellKnownBehavior.InvokeSaga, typeof(SagaPersistenceBehavior), "Invokes the saga logic");
            coordinator.Register(WellKnownBehavior.InvokeHandlers, typeof(InvokeHandlersBehavior), "Calls the IHandleMessages<T>.Handle(T)");
        }

        void RegisterOutgoingCoreBehaviors()
        {
            coordinator.Register(WellKnownBehavior.EnforceBestPractices, typeof(SendValidatorBehavior), "Enforces messaging best practices");
            coordinator.Register("CopySagaHeaders", typeof(SagaSendBehavior), "Copies existing saga headers from incoming message to outgoing message. This facilitates the auto correlation");
            coordinator.Register(WellKnownBehavior.MutateOutgoingMessages, typeof(MutateOutgoingMessageBehavior), "Executes IMutateOutgoingMessages");
            coordinator.Register("DataBusSend", typeof(DataBusSendBehavior), "Saves the payload into the shared location"); //todo: we'll make this optional as soon as we have a way to manipulate the pipeline
            coordinator.Register(WellKnownBehavior.CreatePhysicalMessage, typeof(CreatePhysicalMessageBehavior), "Converts a logical message into a physical message");
            coordinator.Register(WellKnownBehavior.SerializeMessage, typeof(SerializeMessagesBehavior), "Serializes the message to be sent out on the wire");
            coordinator.Register(WellKnownBehavior.MutateOutgoingTransportMessage, typeof(MutateOutgoingPhysicalMessageBehavior), "Executes IMutateOutgoingTransportMessages");
            coordinator.Register(WellKnownBehavior.DispatchMessageToTransport, typeof(DispatchMessageToTransportBehavior), "Dispatches messages to the transport");
        }

        BehaviorRegistrationsCoordinator coordinator;
    }
}