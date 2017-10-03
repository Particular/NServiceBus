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
            var newIdGenerator = GetIdGenerator(context);

            context.Pipeline.Register("AttachCausationHeaders", new AttachCausationHeadersBehavior(newIdGenerator), "Adds related to and conversation id headers to outgoing messages");
        }

        static CustomConversationIdDelegate GetIdGenerator(FeatureConfigurationContext context)
        {
            if (context.Settings.TryGet<CustomConversationIdDelegate>(out var idGenerator))
            {
                return idGenerator;
            }

            return (CustomConversationIdContext _, out string id) =>
            {
                id = null;
                return false;
            };
        }
    }
}