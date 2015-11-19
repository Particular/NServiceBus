namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using MessagingBestPractices;
    using OutgoingPipeline;
    using Pipeline;
    using Routing.MessagingBestPractices;

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

            if (!context.Extensions.TryGet(out options) || options.Enabled)
            {
                validations.AssertIsValidForPubSub(context.Message.MessageType);
            }

            return next();
        }
    }
}