namespace NServiceBus.Features
{
    using Config;
    using NServiceBus.Outbox;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast.Behaviors;

    public class Outbox:Feature
    {
        public override void Initialize()
        {
            InfrastructureServices.Enable<IOutboxStorage>();
        }

        public class PipelineOverride : Pipeline.PipelineOverride
        {
            public override void Override(BehaviorList<ReceivePhysicalMessageContext> behaviorList)
            {
                if (!IsEnabled<Outbox>())
                {
                    return;
                }

                behaviorList.InsertAfter<ChildContainerBehavior, OutboxReceiveBehavior>();
            }


            public override void Override(BehaviorList<SendLogicalMessageContext> behaviorList)
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