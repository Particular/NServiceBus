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

     
            var toSend = new TransportMessage("will be ignored",context.Headers);

            next(new PhysicalOutgoingContextStageBehavior.Context(toSend,context));
        }

    }
}