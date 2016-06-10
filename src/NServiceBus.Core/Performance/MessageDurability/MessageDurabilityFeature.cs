namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DeliveryConstraints;

    class MessageDurabilityFeature : Feature
    {
        public MessageDurabilityFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var conventions = context.Settings.Get<Conventions>();

            var knownMessages = context.Settings.GetAvailableTypes()
                .Where(conventions.IsMessageType)
                .ToList();

            var defaultToDurableMessages = context.Settings.DurableMessagesEnabled();

            var nonDurableMessages = new HashSet<Type>();

            Func<Type, bool> durabilityConvention;

            if (!context.Settings.TryGet("messageDurabilityConvention", out durabilityConvention))
            {
                durabilityConvention = t => t.GetCustomAttributes(typeof(ExpressAttribute), true).Any();
            }

            foreach (var messageType in knownMessages)
            {
                if (!defaultToDurableMessages)
                {
                    nonDurableMessages.Add(messageType);
                }
                else if(durabilityConvention(messageType))
                {
                    nonDurableMessages.Add(messageType);
                }
            }

            if (nonDurableMessages.Any())
            {
                if (!context.DoesTransportSupportConstraint<NonDurableDelivery>())
                {
                    throw new Exception("The configured transport does not support non durable messages but you have configured some messages to be non durable (e.g. by using the [Express] attribute). Make the non durable messages durable or use a transport supporting non durable messages.");
                }

                context.Pipeline.Register(b => new DetermineMessageDurabilityBehavior(nonDurableMessages), "Adds the NonDurableDelivery constraint for messages that have requested to be delivered in non durable mode");
            }
        }
    }
}