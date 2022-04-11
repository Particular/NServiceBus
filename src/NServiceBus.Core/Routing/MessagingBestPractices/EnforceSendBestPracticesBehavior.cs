namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Pipeline;

    class EnforceSendBestPracticesBehavior : IBehavior<IOutgoingSendContext, IOutgoingSendContext>
    {
        public EnforceSendBestPracticesBehavior(Validations validations)
        {
            this.validations = validations;
        }

        public Task Invoke(IOutgoingSendContext context, Func<IOutgoingSendContext, Task> next)
        {
            if (!context.Extensions.TryGetScoped(context.MessageId, out EnforceBestPracticesOptions options) || options.Enabled)
            {
                validations.AssertIsValidForSend(context.Message.MessageType);
            }

            return next(context);
        }

        readonly Validations validations;
    }
}