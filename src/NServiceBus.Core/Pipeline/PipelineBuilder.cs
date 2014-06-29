namespace NServiceBus.Pipeline
{
    using System;
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
            Func<RegisterBehavior, bool> incomingRegisterBehavior = rb => typeof(IBehavior<IncomingContext>).IsAssignableFrom(rb.BehaviorType);
            Func<RegisterBehavior, bool> outgoingRegisterBehavior = rb => typeof(IBehavior<OutgoingContext>).IsAssignableFrom(rb.BehaviorType);

            foreach (var rego in model)
            {
                if (incomingRegisterBehavior(rego))
                {
                    Incoming.Add(rego);
                }

                if (outgoingRegisterBehavior(rego))
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
            coordinator.Register(WellKnownStep.CreateChildContainer, typeof(ChildContainerBehavior), "Creates the child container");
            coordinator.Register(WellKnownStep.ExecuteUnitOfWork, typeof(UnitOfWorkBehavior), "Executes the UoW");
            coordinator.Register("ProcessSubscriptionRequests", typeof(SubscriptionReceiverBehavior), "Check for subscription messages and execute the requested behavior to subscribe or unsubscribe.");
            coordinator.Register(WellKnownStep.MutateIncomingTransportMessage, typeof(ApplyIncomingTransportMessageMutatorsBehavior), "Executes IMutateIncomingTransportMessages");
            coordinator.Register("InvokeRegisteredCallbacks", typeof(CallbackInvocationBehavior), "Updates the callback inmemory dictionary");
            coordinator.Register(WellKnownStep.DeserializeMessages, typeof(DeserializeLogicalMessagesBehavior), "Deserializes the physical message body into logical messages");
            coordinator.Register(WellKnownStep.ExecuteLogicalMessages, typeof(ExecuteLogicalMessagesBehavior), "Starts the execution of each logical message");
            coordinator.Register(WellKnownStep.MutateIncomingMessages, typeof(ApplyIncomingMessageMutatorsBehavior), "Executes IMutateIncomingMessages");
            coordinator.Register(WellKnownStep.LoadHandlers, typeof(LoadHandlersBehavior), "Gets all the handlers to invoke from the MessageHandler registry based on the message type.");
            coordinator.Register("SetCurrentMessageBeingHandled", typeof(SetCurrentMessageBeingHandledBehavior), "Sets the static current message (this is used by the headers)");
            coordinator.Register(WellKnownStep.InvokeHandlers, typeof(InvokeHandlersBehavior), "Calls the IHandleMessages<T>.Handle(T)");
        }

        void RegisterOutgoingCoreBehaviors()    
        {
            coordinator.Register(WellKnownStep.EnforceBestPractices, typeof(SendValidatorBehavior), "Enforces messaging best practices");
            coordinator.Register(WellKnownStep.MutateOutgoingMessages, typeof(MutateOutgoingMessageBehavior), "Executes IMutateOutgoingMessages");
            coordinator.Register("PopulateAutoCorrelationHeadersForReplies", typeof(PopulateAutoCorrelationHeadersForRepliesBehavior), "Copies existing saga headers from incoming message to outgoing message to facilitate the auto correlation in the saga, when replying to a message that was sent by a saga.");
            coordinator.Register(WellKnownStep.CreatePhysicalMessage, typeof(CreatePhysicalMessageBehavior), "Converts a logical message into a physical message");
            coordinator.Register(WellKnownStep.SerializeMessage, typeof(SerializeMessagesBehavior), "Serializes the message to be sent out on the wire");
            coordinator.Register(WellKnownStep.MutateOutgoingTransportMessage, typeof(MutateOutgoingPhysicalMessageBehavior), "Executes IMutateOutgoingTransportMessages");
            if (LogManager.GetLogger("LogOutgoingMessage").IsDebugEnabled)
            {
                coordinator.Register(WellKnownStep.CreateCustom("LogOutgoingMessage"), typeof(LogOutgoingMessageBehavior), "Logs the message contents before it is sent.");
            }
            coordinator.Register(WellKnownStep.DispatchMessageToTransport, typeof(DispatchMessageToTransportBehavior), "Dispatches messages to the transport");
        }

        BehaviorRegistrationsCoordinator coordinator;
    }
}