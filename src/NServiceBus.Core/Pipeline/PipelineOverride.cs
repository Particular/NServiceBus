namespace NServiceBus.Pipeline
{
    using Contexts;

    abstract class PipelineOverride 
    {
        public virtual void Override(BehaviorList<HandlerInvocationContext> behaviorList)
        {
        }
        public virtual void Override(BehaviorList<ReceiveLogicalMessageContext> behaviorList)
        {
        }
        public virtual void Override(BehaviorList<ReceivePhysicalMessageContext> behaviorList)
        {
        }
        public virtual void Override(BehaviorList<SendLogicalMessageContext> behaviorList)
        {
        }
        public virtual void Override(BehaviorList<SendLogicalMessagesContext> behaviorList)
        {
        }
        public virtual void Override(BehaviorList<SendPhysicalMessageContext> behaviorList)
        {
        }
    }
}