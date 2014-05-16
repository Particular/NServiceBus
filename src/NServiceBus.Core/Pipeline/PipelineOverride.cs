namespace NServiceBus.Pipeline
{
    using Contexts;


    public abstract class PipelineOverride : IPipelineOverride
    {
        public virtual void Override(BehaviorList<IncomingContext> behaviorList)
        {
        }
        public virtual void Override(BehaviorList<OutgoingContext> behaviorList)
        {
        }
    }
}