namespace NServiceBus.Features
{
    class MessageCausation : Feature
    {
        public MessageCausation()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register("AttachCausationHeaders", new AttachCausationHeadersBehavior(), "Adds related to and conversation id headers to outgoing messages");
        }
    }
}