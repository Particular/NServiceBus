namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DeliveryConstraints;
    using Pipeline;
    using Pipeline.Contexts;
    using TransportDispatch;

    class DetermineMessageDurabilityBehavior : Behavior<OutgoingLogicalMessageContext>
    {
        public DetermineMessageDurabilityBehavior(Dictionary<Type,bool> durabilitySettings)
        {
            this.durabilitySettings = durabilitySettings;
        }

        public override Task Invoke(OutgoingLogicalMessageContext context, Func<Task> next)
        {
            bool isDurable;
            if (durabilitySettings.TryGetValue(context.Message.MessageType, out isDurable) && !isDurable)
            {
                context.AddDeliveryConstraint(new NonDurableDelivery());

                context.SetHeader(Headers.NonDurableMessage,true.ToString());
            }

            return next();
        }

        Dictionary<Type, bool> durabilitySettings;
    }
}