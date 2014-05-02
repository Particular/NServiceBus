namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using Audit;
    using Contexts;
    using DataBus;
    using MessageMutator;
    using NServiceBus.MessageMutator;
    using ObjectBuilder;
    using Sagas;
    using Unicast.Behaviors;
    using Unicast.Messages;
    using UnitOfWork;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class PipelineBuilder
    {
        List<IPipelineOverride> pipelineOverrides;
        public List<Type> sendPhysicalMessageBehaviorList { get; private set; }
        public List<Type> receivePhysicalMessageBehaviorList { get; private set; }
        public List<Type> receiveLogicalMessageBehaviorList { get; private set; }
        public List<Type> handlerInvocationBehaviorList { get; private set; }
        public List<Type> sendLogicalMessagesBehaviorList { get; private set; }
        public List<Type> sendLogicalMessageBehaviorList { get; private set; }

        public PipelineBuilder(IBuilder builder)
        {
            pipelineOverrides = builder.BuildAll<IPipelineOverride>().ToList();
            CreateSendPhysicalMessageList();
            CreateReceivePhysicalMessageList();
            CreateReceiveLogicalMessageList();
            CreateHandlerInvocationList();
            CreateSendLogicalMessagesList();
            CreateSendLogicalMessageList();
        }

        void CreateSendPhysicalMessageList()
        {
            var behaviorList = new BehaviorList<SendPhysicalMessageContext>();

            behaviorList.Add<SerializeMessagesBehavior>();
            behaviorList.Add<MutateOutgoingPhysicalMessageBehavior>();
            behaviorList.Add<DispatchMessageToTransportBehavior>();

            foreach (var pipelineOverride in pipelineOverrides)
            {
                pipelineOverride.Override(behaviorList);
            }

            sendPhysicalMessageBehaviorList = behaviorList.InnerList;
        }

        void CreateReceivePhysicalMessageList()
        {
            var behaviorList = new BehaviorList<ReceivePhysicalMessageContext>();

            behaviorList.Add<ChildContainerBehavior>();
            behaviorList.Add<MessageHandlingLoggingBehavior>();
            behaviorList.Add<AuditBehavior>();
            behaviorList.Add<ForwardBehavior>();
            behaviorList.Add<UnitOfWorkBehavior>();
            behaviorList.Add<ApplyIncomingTransportMessageMutatorsBehavior>();
            behaviorList.Add<RaiseMessageReceivedBehavior>();
            behaviorList.Add<RemoveIncomingHeadersBehavior>();
            behaviorList.Add<ExtractLogicalMessagesBehavior>();
            behaviorList.Add<CallbackInvocationBehavior>();
            behaviorList.Add<ExecuteLogicalMessagesBehavior>();

            foreach (var pipelineOverride in pipelineOverrides)
            {
                pipelineOverride.Override(behaviorList);
            }
            receivePhysicalMessageBehaviorList = behaviorList.InnerList;
        }

        void CreateReceiveLogicalMessageList()
        {
            var behaviorList = new BehaviorList<ReceiveLogicalMessageContext>();
            behaviorList.Add<ApplyIncomingMessageMutatorsBehavior>();
            //todo: we'll make this optional as soon as we have a way to manipulate the pipeline
            behaviorList.Add<DataBusReceiveBehavior>();
            behaviorList.Add<LoadHandlersBehavior>();

            foreach (var pipelineOverride in pipelineOverrides)
            {
                pipelineOverride.Override(behaviorList);
            }
            receiveLogicalMessageBehaviorList = behaviorList.InnerList;
        }

        void CreateHandlerInvocationList()
        {
            var behaviorList = new BehaviorList<HandlerInvocationContext>();

            behaviorList.Add<SetCurrentMessageBeingHandledBehavior>();
            behaviorList.Add<AuditInvokedSagaBehavior>();
            behaviorList.Add<SagaPersistenceBehavior>();
            behaviorList.Add<InvokeHandlersBehavior>();

            foreach (var pipelineOverride in pipelineOverrides)
            {
                pipelineOverride.Override(behaviorList);
            }
            handlerInvocationBehaviorList = behaviorList.InnerList;
        }

        void CreateSendLogicalMessagesList()
        {
            var behaviorList = new BehaviorList<SendLogicalMessagesContext>();

            behaviorList.Add<MultiSendValidatorBehavior>();
            behaviorList.Add<MultiMessageBehavior>();
            behaviorList.Add<CreatePhysicalMessageBehavior>();

            foreach (var pipelineOverride in pipelineOverrides)
            {
                pipelineOverride.Override(behaviorList);
            }
            sendLogicalMessagesBehaviorList = behaviorList.InnerList;
        }

        void CreateSendLogicalMessageList()
        {
            var behaviorList = new BehaviorList<SendLogicalMessageContext>();

            behaviorList.Add<SendValidatorBehavior>();
            behaviorList.Add<SagaSendBehavior>();
            behaviorList.Add<MutateOutgoingMessageBehavior>();
            //todo: we'll make this optional as soon as we have a way to manipulate the pipeline
            behaviorList.Add<DataBusSendBehavior>();

            foreach (var pipelineOverride in pipelineOverrides)
            {
                pipelineOverride.Override(behaviorList);
            }
            sendLogicalMessageBehaviorList = behaviorList.InnerList;
        }


    }
}