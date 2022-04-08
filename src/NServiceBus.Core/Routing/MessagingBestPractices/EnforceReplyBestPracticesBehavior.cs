namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Pipeline;

    class EnforceReplyBestPracticesBehavior : IBehavior<IOutgoingReplyContext, IOutgoingReplyContext>
    {
        public EnforceReplyBestPracticesBehavior(Validations validations)
        {
            this.validations = validations;
        }

        public Task Invoke(IOutgoingReplyContext context, Func<IOutgoingReplyContext, Task> next)
        {
            if (!context.Extensions.TryGet(ContextBag.GetPrefixedKey<EnforceBestPracticesOptions>(context.MessageId), out EnforceBestPracticesOptions options)
                || options.Enabled)
            {
                validations.AssertIsValidForReply(context.Message.MessageType);
            }

            return next(context);
        }

        readonly Validations validations;
    }
}