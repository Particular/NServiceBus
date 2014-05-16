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
    using UnitOfWork;

    internal class PipelineBuilder
    {
        public PipelineBuilder()
        {
            var removals = SettingsHolder.Get<List<RemoveBehavior>>("Pipeline.Removals");
            var replacements = SettingsHolder.Get<List<ReplaceBehavior>>("Pipeline.Replacements");

            coordinator = new BehaviorRegistrationsCoordinator(removals, replacements);

            RegisterIncomingCoreBehaviors();
            RegisterOutgoingCoreBehaviors();
            RegisterAdditionalBehaviors();

            var model = coordinator.BuildRuntimeModel();
            Incoming = new List<RegisterBehavior>();
            Outgoing = new List<RegisterBehavior>();
            var incomingContextType = typeof(IBehavior<IncomingContext>);
            var outgoingContextType = typeof(IBehavior<OutgoingContext>);

            foreach (var rego in model)
            {
                if (rego.BehaviorType.IsAssignableFrom(incomingContextType))
                {
                    Incoming.Add(rego);
                }

                if (rego.BehaviorType.IsAssignableFrom(outgoingContextType))
                {
                    Outgoing.Add(rego);
                }
            }
        }

        public List<RegisterBehavior> Incoming { get; private set; }
        public List<RegisterBehavior> Outgoing { get; private set; }

        void RegisterAdditionalBehaviors()
        {
            var additions = SettingsHolder.Get<List<RegisterBehavior>>("Pipeline.Additions");

            foreach (var rego in additions)
            {
                coordinator.Register(rego);
            }
        }

        void RegisterIncomingCoreBehaviors()
        {
            coordinator.Register(WellKnownBehavior.ChildContainer, typeof(ChildContainerBehavior), "Creates the child container");
            coordinator.Register("MessageReceivedLogging", typeof(MessageHandlingLoggingBehavior), "Logs the message received");
            coordinator.Register("ForwardMessageToAuditQueue", typeof(AuditBehavior), "Forward message to audit queue after message is successfully processed");
            coordinator.Register("ForwardMessageTo", typeof(ForwardBehavior), "Forwards message to");
            coordinator.Register(WellKnownBehavior.UnitOfWork, typeof(UnitOfWorkBehavior), "Executes the UoW");
            coordinator.Register(WellKnownBehavior.IncomingTransportMessageMutators, typeof(ApplyIncomingTransportMessageMutatorsBehavior), "Executes the IMutateIncomingTransportMessages");
            coordinator.Register("RemoveHeaders", typeof(RemoveIncomingHeadersBehavior), "For backward compatibility we need to remove some headers from the incoming message");
            coordinator.Register("CallBack", typeof(CallbackInvocationBehavior), "Updates the callback inmemory dictionary");
            coordinator.Register("ExtractLogicalMessages", typeof(ExtractLogicalMessagesBehavior), "ExtractLogicalMessagesBehavior");
            coordinator.Register("ExecuteLogicalMessages", typeof(ExecuteLogicalMessagesBehavior), "ExecuteLogicalMessagesBehavior");
            coordinator.Register("ApplyIncomingMessageMutators", typeof(ApplyIncomingMessageMutatorsBehavior), "ApplyIncomingMessageMutatorsBehavior");
            coordinator.Register("DataBusReceive", typeof(DataBusReceiveBehavior), "DataBusReceiveBehavior"); //todo: we'll make this optional as soon as we have a way to manipulate the pipeline
            coordinator.Register("LoadHandlers", typeof(LoadHandlersBehavior), "LoadHandlersBehavior");
            coordinator.Register("SetCurrentMessageBeingHandled", typeof(SetCurrentMessageBeingHandledBehavior), "SetCurrentMessageBeingHandledBehavior");
            coordinator.Register("AuditInvokedSaga", typeof(AuditInvokedSagaBehavior), "AuditInvokedSagaBehavior");
            coordinator.Register("SagaPersistence", typeof(SagaPersistenceBehavior), "SagaPersistenceBehavior");
            coordinator.Register("InvokeHandlers", typeof(InvokeHandlersBehavior), "InvokeHandlersBehavior");
        }

        void RegisterOutgoingCoreBehaviors()
        {
            coordinator.Register("SendValidator", typeof(SendValidatorBehavior), "SendValidatorBehavior");
            coordinator.Register("SagaSend", typeof(SagaSendBehavior), "SagaSendBehavior");
            coordinator.Register("MutateOutgoingMessageBehavior", typeof(MutateOutgoingMessageBehavior), "InvokeHandlersBehavior");
            coordinator.Register("DataBusSendBehavior", typeof(DataBusSendBehavior), "InvokeHandlersBehavior"); //todo: we'll make this optional as soon as we have a way to manipulate the pipeline
            coordinator.Register("CreatePhysicalMessageBehavior", typeof(CreatePhysicalMessageBehavior), "InvokeHandlersBehavior");
            coordinator.Register("SerializeMessagesBehavior", typeof(SerializeMessagesBehavior), "InvokeHandlersBehavior");
            coordinator.Register("MutateOutgoingPhysicalMessageBehavior", typeof(MutateOutgoingPhysicalMessageBehavior), "InvokeHandlersBehavior");
            coordinator.Register("DispatchMessageToTransportBehavior", typeof(DispatchMessageToTransportBehavior), "InvokeHandlersBehavior");
        }

        BehaviorRegistrationsCoordinator coordinator;
    }
}