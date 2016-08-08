namespace NServiceBus.Features
{
    using System;
    using DeliveryConstraints;

    class MessageDurabilityFeature : Feature
    {
        public MessageDurabilityFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            defaultToDurableMessages = context.Settings.DurableMessagesEnabled();

            if (!context.Settings.TryGet("messageDurabilityConvention", out durabilityConvention))
            {
                durabilityConvention = t => t.GetCustomAttributes(typeof(ExpressAttribute), true).Length > 0;
            }

            doesSupportNonDurableDelivery = context.Settings.DoesTransportSupportConstraint<NonDurableDelivery>();

            if (!defaultToDurableMessages && !doesSupportNonDurableDelivery)
            {
                throw new Exception(DoesNotSupportNonDurableDeliveryExceptionMessage);
            }

            context.Pipeline.Register(new DetermineMessageDurabilityBehavior(t => Convention(t, doesSupportNonDurableDelivery, defaultToDurableMessages, durabilityConvention)), "Adds the NonDurableDelivery constraint for messages that have requested to be delivered in non-durable mode");
        }

        public static bool Convention(Type messageType, bool doesSupportNonDurableDelivery, bool defaultToDurableMessages, Func<Type, bool> durabilityConvention)
        {
            var nonDurable = !defaultToDurableMessages || durabilityConvention(messageType);
            if (nonDurable && !doesSupportNonDurableDelivery)
            {
                throw new Exception(DoesNotSupportNonDurableDeliveryExceptionMessage);
            }
            return nonDurable;
        }

        // fields here opt-in for delegate caching.
        bool defaultToDurableMessages;
        bool doesSupportNonDurableDelivery;
        Func<Type, bool> durabilityConvention;
        const string DoesNotSupportNonDurableDeliveryExceptionMessage = "The configured transport does not support non-durable messages but some messages have been configured to be non-durable (e.g. by using the [Express] attribute). Make the messages durable, or use a transport supporting non-durable messages.";
    }
}