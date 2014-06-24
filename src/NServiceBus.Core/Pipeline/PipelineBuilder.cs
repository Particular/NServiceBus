namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using Contexts;
    using MessageMutator;
    using Logging;
    using NServiceBus.MessageMutator;
    using Unicast.Behaviors;
    using Unicast.Messages;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using UnitOfWork;
    using Sagas;

    class PipelineBuilder
    {
        public PipelineBuilder(PipelineModifications modifications)
        {
            coordinator = new BehaviorRegistrationsCoordinator(modifications.Removals, modifications.Replacements);

            RegisterIncomingCoreBehaviors();
            RegisterOutgoingCoreBehaviors();
            RegisterAdditionalBehaviors(modifications.Additions);

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

        void RegisterAdditionalBehaviors(List<RegisterBehavior> additions)
        {
            foreach (var rego in additions)
            {
                coordinator.Register(rego);
            }
        }

        void RegisterIncomingCoreBehaviors()
        {
            coordinator.Register(WellKnownBehavior.CreateChildContainer, typeof(ChildContainerBehavior), "Creates the child container");
            coordinator.Register(WellKnownBehavior.ExecuteUnitOfWork, typeof(UnitOfWorkBehavior), "Executes the UoW");
            coordinator.Register("ProcessSubscriptionRequests", typeof(SubscriptionReceiverBehavior), "Check for subscription messages and execute the requested behavior to subscribe or unsubscribe.");
            coordinator.Register(WellKnownBehavior.MutateIncomingTransportMessage, typeof(ApplyIncomingTransportMessageMutatorsBehavior), "Executes IMutateIncomingTransportMessages");
            coordinator.Register("InvokeRegisteredCallbacks", typeof(CallbackInvocationBehavior), "Updates the callback inmemory dictionary");
            coordinator.Register(WellKnownBehavior.DeserializeMessages, typeof(DeserializeLogicalMessagesBehavior), "Deserializes the physical message body into logical messages");
            coordinator.Register(WellKnownBehavior.ExecuteLogicalMessages, typeof(ExecuteLogicalMessagesBehavior), "Starts the execution of each logical message");
            coordinator.Register(WellKnownBehavior.MutateIncomingMessages, typeof(ApplyIncomingMessageMutatorsBehavior), "Executes IMutateIncomingMessages");
            coordinator.Register(WellKnownBehavior.LoadHandlers, typeof(LoadHandlersBehavior), "Gets all the handlers to invoke from the MessageHandler registry based on the message type.");
            coordinator.Register("SetCurrentMessageBeingHandled", typeof(SetCurrentMessageBeingHandledBehavior), "Sets the static current message (this is used by the headers)");
            coordinator.Register(WellKnownBehavior.InvokeHandlers, typeof(InvokeHandlersBehavior), "Calls the IHandleMessages<T>.Handle(T)");
        }

        void RegisterOutgoingCoreBehaviors()    
        {
            coordinator.Register(WellKnownBehavior.EnforceBestPractices, typeof(SendValidatorBehavior), "Enforces messaging best practices");
            coordinator.Register(WellKnownBehavior.MutateOutgoingMessages, typeof(MutateOutgoingMessageBehavior), "Executes IMutateOutgoingMessages");
            coordinator.Register("PopulateAutoCorrelationHeadersForReplies", typeof(PopulateAutoCorrelationHeadersForRepliesBehavior), "Copies existing saga headers from incoming message to outgoing message to facilitate the auto correlation in the saga, when replying to a message that was sent by a saga.");
            coordinator.Register(WellKnownBehavior.CreatePhysicalMessage, typeof(CreatePhysicalMessageBehavior), "Converts a logical message into a physical message");
            coordinator.Register(WellKnownBehavior.SerializeMessage, typeof(SerializeMessagesBehavior), "Serializes the message to be sent out on the wire");
            coordinator.Register(WellKnownBehavior.MutateOutgoingTransportMessage, typeof(MutateOutgoingPhysicalMessageBehavior), "Executes IMutateOutgoingTransportMessages");
            if (LogManager.GetLogger("LogOutgoingMessage").IsDebugEnabled)
            {
                coordinator.Register("LogOutgoingMessage", typeof(LogOutgoingMessageBehavior), "Logs the message contents before it is sent.");
            }
            coordinator.Register(WellKnownBehavior.DispatchMessageToTransport, typeof(DispatchMessageToTransportBehavior), "Dispatches messages to the transport");
        }

        BehaviorRegistrationsCoordinator coordinator;
    }
}