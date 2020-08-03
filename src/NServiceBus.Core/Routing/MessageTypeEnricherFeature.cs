namespace NServiceBus.Features
{
    class MessageTypeEnricherFeature : Feature
    {
        public MessageTypeEnricherFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register(typeof(MessageTypeEnricherBehavior), "Adds EnclosedMessageType to the header of the message if it doesn't exist.");
        }
    }
}