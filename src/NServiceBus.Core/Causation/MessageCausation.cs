namespace NServiceBus.Features
{
    class MessageCausation:Feature
    {
        public MessageCausation()
        {
            EnableByDefault();
        }
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register("AttachCausationHeaders", typeof(AttachCausationHeadersBehavior), "Adds related to and conversation id headers to outgoing messages");
        }
    }
}