namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.TransportDispatch;

    class DetermineMessageDurabilityBehavior:Behavior<OutgoingContext>
    {
        
        public DetermineMessageDurabilityBehavior(Dictionary<Type,bool> durabilitySettings)
        {
            this.durabilitySettings = durabilitySettings;
        }

        public override void Invoke(OutgoingContext context, Action next)
        {
            bool isDurable;
            if (durabilitySettings.TryGetValue(context.GetMessageType(), out isDurable) && !isDurable)
            {
                context.AddDeliveryConstraint(new NonDurableDelivery());

                context.SetHeader(Headers.NonDurableMessage,true.ToString());
            }

            next();
        }

        readonly Dictionary<Type, bool> durabilitySettings;
    }
}