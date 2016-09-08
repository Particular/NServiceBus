namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class EnforceReplyBestPracticesBehavior : IBehavior<IOutgoingReplyContext, IOutgoingReplyContext>
    {
        public EnforceReplyBestPracticesBehavior(Validations validations)
        {
            this.validations = validations;
        }

        public Task Invoke(IOutgoingReplyContext context, Func<IOutgoingReplyContext, Task> next)
        {
            EnforceBestPracticesOptions options;

            if (!context.Extensions.TryGet(out options) || options.Enabled)
            {
                validations.AssertIsValidForReply(context.Message.MessageType);
            }

            return next(context);
        }

        Validations validations;
    }
}