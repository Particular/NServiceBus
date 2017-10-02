namespace NServiceBus.Features
{
    using System;

    class MessageCausation : Feature
    {
        public MessageCausation()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            if (!context.Settings.TryGet<Func<ConversationIdGeneratorContext, string>>(MessageCausationConfigurationExtensions.CustomConversationIdGeneratorKey, out var idGenerator))
            {
                idGenerator = _ => CombGuid.Generate().ToString();
            }

            context.Pipeline.Register("AttachCausationHeaders", new AttachCausationHeadersBehavior(idGenerator), "Adds related to and conversation id headers to outgoing messages");
        }
    }
}