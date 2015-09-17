namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.MessagingBestPractices;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing.MessagingBestPractices;

    class EnforceSendBestPracticesBehavior : Behavior<OutgoingSendContext>
    {
        Validations validations;

        public EnforceSendBestPracticesBehavior(Validations validations)
        {
            this.validations = validations;
        }

        public override Task Invoke(OutgoingSendContext context, Func<Task> next)
        {
            EnforceBestPracticesOptions options;

            if (!context.TryGet(out options) || options.Enabled)
            {
                validations.AssertIsValidForSend(context.GetMessageType());
            }

            return next();
        }
    }
}