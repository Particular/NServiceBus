namespace NServiceBus.Recoverability
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;

    class InvokeRecoverabilityPolicyBehavior : IBehavior<IRecoverabilityContext, IRecoverabilityContext>
    {
        public InvokeRecoverabilityPolicyBehavior(RecoverabilityExecutor recoverabilityExecutor)
        {
            this.recoverabilityExecutor = recoverabilityExecutor;
        }

        public async Task Invoke(IRecoverabilityContext context, Func<IRecoverabilityContext, Task> next)
        {
            context.ActionToTake = await recoverabilityExecutor.Invoke(context.ErrorContext).ConfigureAwait(false);

            await next(context).ConfigureAwait(false);
        }

        readonly RecoverabilityExecutor recoverabilityExecutor;
    }
}
