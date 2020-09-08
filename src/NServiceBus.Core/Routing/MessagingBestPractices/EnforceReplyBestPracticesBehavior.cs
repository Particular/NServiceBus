namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;

    class EnforceReplyBestPracticesBehavior : IBehavior<IOutgoingReplyContext, IOutgoingReplyContext>
    {
        public EnforceReplyBestPracticesBehavior(Validations validations)
        {
            this.validations = validations;
        }

        public Task Invoke(IOutgoingReplyContext context, Func<IOutgoingReplyContext, CancellationToken, Task> next, CancellationToken cancellationToken)
        {
            if (!context.Extensions.TryGet(out EnforceBestPracticesOptions options) || options.Enabled)
            {
                validations.AssertIsValidForReply(context.Message.MessageType);
            }

            return next(context, cancellationToken);
        }

        readonly Validations validations;
    }
}