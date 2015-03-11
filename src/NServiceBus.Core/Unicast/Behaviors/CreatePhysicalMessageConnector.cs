namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast;

    class CreatePhysicalMessageConnector : StageConnector<OutgoingContext, PhysicalOutgoingContextStageBehavior.Context>
    {
        readonly Configure configure;

        public CreatePhysicalMessageConnector(Configure configure)
        {
            this.configure = configure;
        }

        public override void Invoke(OutgoingContext context, Action<PhysicalOutgoingContextStageBehavior.Context> next)
        {

            var intent = MessageIntentEnum.Publish;

            if (context.DeliveryOptions is SendOptions)
            {
                intent = MessageIntentEnum.Send;   
            }
     
            var toSend = new TransportMessage { MessageIntent = intent };

            if (context.DeliveryOptions is ReplyOptions)
            {
                intent = MessageIntentEnum.Reply;
            }
     
            var toSend = new TransportMessage { MessageIntent = intent };

            next(new PhysicalOutgoingContextStageBehavior.Context(toSend,context));
        }

    }
}