namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using OutgoingPipeline;
    using Pipeline;

    class EnforceSendBestPracticesBehavior : Behavior<IOutgoingSendContext>
    {
        public EnforceSendBestPracticesBehavior(Validations validations)
        {
            this.validations = validations;
        }

        public override Task Invoke(IOutgoingSendContext context, Func<Task> next)
        {
            EnforceBestPracticesOptions options;

            if (!context.Extensions.TryGet(out options) || options.Enabled)
            {
                validations.AssertIsValidForSend(context.Message.MessageType);
            }

            return next();
        }

        Validations validations;
    }
}