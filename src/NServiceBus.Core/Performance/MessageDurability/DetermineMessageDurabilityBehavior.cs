namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DeliveryConstraints;
    using Pipeline;

    class DetermineMessageDurabilityBehavior : Behavior<IOutgoingLogicalMessageContext>
    {
        public DetermineMessageDurabilityBehavior(HashSet<Type> nonDurableMessages)
        {
            this.nonDurableMessages = nonDurableMessages;
        }

        public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
        {
            if (nonDurableMessages.Contains(context.Message.MessageType))
            {
                context.Extensions.AddDeliveryConstraint(new NonDurableDelivery());

                context.Headers[Headers.NonDurableMessage] = true.ToString();
            }

            return next();
        }

        readonly HashSet<Type> nonDurableMessages;
    }
}