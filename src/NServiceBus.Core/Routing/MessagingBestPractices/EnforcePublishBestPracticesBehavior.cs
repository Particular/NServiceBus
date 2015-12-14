namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using OutgoingPipeline;
    using Pipeline;

    class EnforcePublishBestPracticesBehavior : Behavior<IOutgoingPublishContext>
    {
        Validations validations;

        public EnforcePublishBestPracticesBehavior(Validations validations)
        {
            this.validations = validations;
        }

        public override Task Invoke(IOutgoingPublishContext context, Func<Task> next)
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