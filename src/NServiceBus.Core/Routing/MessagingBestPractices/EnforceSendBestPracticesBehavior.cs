namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using MessagingBestPractices;
    using OutgoingPipeline;
    using Pipeline;
    using Routing.MessagingBestPractices;

    class EnforceSendBestPracticesBehavior : Behavior<OutgoingSendContext>
    {
        public EnforceSendBestPracticesBehavior(Validations validations)
        {
            this.validations = validations;
        }

        public override Task Invoke(OutgoingSendContext context, Func<Task> next)
        {
            EnforceBestPracticesOptions options;

            if (!context.TryGet(out options) || options.Enabled)
            {
                validations.AssertIsValidForSend(context.Message.MessageType);
            }

            return next();
        }

        Validations validations;
    }
}