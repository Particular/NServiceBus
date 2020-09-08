namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;

    class EnforceSendBestPracticesBehavior : IBehavior<IOutgoingSendContext, IOutgoingSendContext>
    {
        public EnforceSendBestPracticesBehavior(Validations validations)
        {
            this.validations = validations;
        }

        public Task Invoke(IOutgoingSendContext context, Func<IOutgoingSendContext, Task> next, CancellationToken cancellationToken)
        {
            if (!context.Extensions.TryGet(out EnforceBestPracticesOptions options) || options.Enabled)
            {
                validations.AssertIsValidForSend(context.Message.MessageType);
            }

            return next(context);
        }

        readonly Validations validations;
    }
}