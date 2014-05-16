namespace NServiceBus.Features
{
    using NServiceBus.Outbox;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast.Behaviors;
    using UnitOfWork;

    public class Outbox : Feature
    {
        public override void Initialize(Configure config)
        {
             
        }

        public class PipelineOverride : Pipeline.PipelineOverride
        {
            public override void Override(BehaviorList<IncomingContext> behaviorList)
            {
                if (!IsEnabled<Outbox>())
                {
                    return;
                }

                behaviorList.InsertAfter<ChildContainerBehavior, OutboxDeduplicationBehavior>();
                behaviorList.InsertAfter<UnitOfWorkBehavior, OutboxRecordBehavior>();
            }

            public override void Override(BehaviorList<OutgoingContext> behaviorList)
            {
                if (!IsEnabled<Outbox>())
                {
                    return;
                }

                behaviorList.Replace<DispatchMessageToTransportBehavior, OutboxSendBehavior>();
            }
        }
    }
}
