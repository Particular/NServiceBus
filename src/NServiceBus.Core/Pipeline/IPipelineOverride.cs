namespace NServiceBus.Pipeline
{
    using Contexts;

    public interface IPipelineOverride
    {
        void Override(BehaviorList<IncomingContext> behaviorList);
        void Override(BehaviorList<OutgoingContext> behaviorList);
    }
}