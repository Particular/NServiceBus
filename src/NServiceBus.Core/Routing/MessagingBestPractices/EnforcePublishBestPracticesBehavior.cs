namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
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

        public override Task Invoke(OutgoingPublishContext context, Func<Task> next)
        {
            EnforceBestPracticesOptions options;

            if (!context.TryGet(out options) || options.Enabled)
            {
                validations.AssertIsValidForPubSub(context.GetMessageType());
            }

            return next();
        }
    }
}