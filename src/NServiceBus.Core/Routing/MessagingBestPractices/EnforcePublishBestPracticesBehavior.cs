namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class EnforcePublishBestPracticesBehavior : IBehavior<IOutgoingPublishContext, IOutgoingPublishContext>
    {
        public EnforcePublishBestPracticesBehavior(Validations validations)
        {
            this.validations = validations;
        }

        public Task Invoke(IOutgoingPublishContext context, Func<IOutgoingPublishContext, Task> next)
        {
            EnforceBestPracticesOptions options;

            if (!context.Extensions.TryGet(out options) || options.Enabled)
            {
                validations.AssertIsValidForPubSub(context.Message.MessageType);
            }

            return next(context);
        }

        Validations validations;
    }
}