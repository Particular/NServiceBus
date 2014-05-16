namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using Audit;
    using Contexts;
    using DataBus;
    using MessageMutator;
    using NServiceBus.MessageMutator;
    using Sagas;
    using Unicast.Behaviors;
    using Unicast.Messages;
    using UnitOfWork;

    public class PipelineBuilder
    {
        public PipelineBuilder()
        {
            CreateReceivePhysicalMessageList();
            CreateSendLogicalMessageList();
        }

        public List<Type> sendPhysicalMessageBehaviorList { get; private set; }
        public List<Type> receivePhysicalMessageBehaviorList { get; private set; }
        public List<Type> handlerInvocationBehaviorList { get; private set; }
        public List<Type> sendLogicalMessageBehaviorList { get; private set; }

        void CreateReceivePhysicalMessageList()
        {
            var behaviorList = new BehaviorList<IncomingContext>();

            behaviorList.Add<ChildContainerBehavior>();
            behaviorList.Add<MessageHandlingLoggingBehavior>();
            behaviorList.Add<AuditBehavior>();
            behaviorList.Add<ForwardBehavior>();
            behaviorList.Add<UnitOfWorkBehavior>();
            behaviorList.Add<ApplyIncomingTransportMessageMutatorsBehavior>();
            behaviorList.Add<RemoveIncomingHeadersBehavior>();
            behaviorList.Add<CallbackInvocationBehavior>();
            behaviorList.Add<ExtractLogicalMessagesBehavior>();
            behaviorList.Add<ExecuteLogicalMessagesBehavior>();

            behaviorList.Add<ApplyIncomingMessageMutatorsBehavior>();
            //todo: we'll make this optional as soon as we have a way to manipulate the pipeline
            behaviorList.Add<DataBusReceiveBehavior>();
            behaviorList.Add<LoadHandlersBehavior>();
            behaviorList.Add<SetCurrentMessageBeingHandledBehavior>();
            behaviorList.Add<AuditInvokedSagaBehavior>();
            behaviorList.Add<SagaPersistenceBehavior>();
            behaviorList.Add<InvokeHandlersBehavior>();
            receivePhysicalMessageBehaviorList = behaviorList.InnerList;
        }

        void CreateSendLogicalMessageList()
        {
            var behaviorList = new BehaviorList<OutgoingContext>();

            behaviorList.Add<SendValidatorBehavior>();
            behaviorList.Add<SagaSendBehavior>();
            behaviorList.Add<MutateOutgoingMessageBehavior>();
            //todo: we'll make this optional as soon as we have a way to manipulate the pipeline
            behaviorList.Add<DataBusSendBehavior>();
            behaviorList.Add<CreatePhysicalMessageBehavior>();
            behaviorList.Add<SerializeMessagesBehavior>();
            behaviorList.Add<MutateOutgoingPhysicalMessageBehavior>();
            behaviorList.Add<DispatchMessageToTransportBehavior>();
            sendLogicalMessageBehaviorList = behaviorList.InnerList;
        }
    }
}