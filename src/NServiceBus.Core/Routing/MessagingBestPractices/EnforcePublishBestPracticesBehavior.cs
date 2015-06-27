namespace NServiceBus
{
    using System;
    using NServiceBus.MessagingBestPractices;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing.MessagingBestPractices;

    class EnforcePublishBestPracticesBehavior : Behavior<OutgoingPublishContext>
    {
        Validations validations;

        public EnforcePublishBestPracticesBehavior(Validations validations)
        {
            this.validations = validations;
        }

        public override void Invoke(OutgoingPublishContext context, Action next)
        {
            EnforceBestPracticesOptions options;

            if (!context.TryGet(out options) || options.Enabled)
            {
                validations.AssertIsValidForPubSub(context.GetMessageType());
            }

            next();
        }
    }
}