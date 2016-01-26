namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DeliveryConstraints;
    using Pipeline;

    class DetermineMessageDurabilityBehavior : Behavior<IOutgoingLogicalMessageContext>
    {
        public DetermineMessageDurabilityBehavior(Dictionary<Type,bool> durabilitySettings)
        {
            this.durabilitySettings = durabilitySettings;
        }

        public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
        {
            bool isDurable;
            if (durabilitySettings.TryGetValue(context.Message.MessageType, out isDurable) && !isDurable)
            {
                context.AddDeliveryConstraint(new NonDurableDelivery());

                context.Headers[Headers.NonDurableMessage] = true.ToString();
            }

            return next();
        }

        Dictionary<Type, bool> durabilitySettings;
    }
}