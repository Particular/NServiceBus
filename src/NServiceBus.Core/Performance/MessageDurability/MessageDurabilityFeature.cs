namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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

            var defaultToDurableMessages = DurableMessagesConfig.GetDurableMessagesEnabled(context.Settings);

            var messageDurability = new Dictionary<Type, bool>();

            Func<Type, bool> durabilityConvention;

            if (!context.Settings.TryGet("messageDurabilityConvention", out durabilityConvention))
            {
                durabilityConvention = t => t.GetCustomAttributes(typeof(ExpressAttribute), true).Any();
            }

            foreach (var messageType in knownMessages)
            {
                var isDurable = defaultToDurableMessages;

                if (durabilityConvention(messageType))
                {
                    isDurable = false;
                }

                messageDurability[messageType] = isDurable;
            }

            context.Pipeline.Register("DetermineMessageDurability", typeof(DetermineMessageDurabilityBehavior), "Adds the NonDurableDelivery constraint for messages that have requested to be delivered in non durable mode");


            context.Container.ConfigureComponent(b => new DetermineMessageDurabilityBehavior(messageDurability), DependencyLifecycle.SingleInstance);
        }
    }
}