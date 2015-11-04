namespace NServiceBus.Features
{
    using System.Collections.Generic;

    class MessageCausation:Feature
    {
        public MessageCausation()
        {
            EnableByDefault();
        }
        protected internal override IReadOnlyCollection<FeatureStartupTask> Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register("AttachCausationHeaders", typeof(AttachCausationHeadersBehavior), "Adds related to and conversation id headers to outgoing messages");

            return FeatureStartupTask.None;
        }
    }
}