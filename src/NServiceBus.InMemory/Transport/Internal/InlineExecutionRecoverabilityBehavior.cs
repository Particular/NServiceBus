namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;
using Transport;

sealed class InlineExecutionRecoverabilityBehavior(InlineExecutionSettings settings) : IBehavior<IRecoverabilityContext, IRecoverabilityContext>
{
    public async Task Invoke(IRecoverabilityContext context, Func<IRecoverabilityContext, Task> next)
    {
        if (!context.Extensions.TryGet<TransportTransaction>(out var transportTransaction) || !transportTransaction.TryGet<InlineExecutionDispatchContext>(out var dispatch))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var scope = dispatch.Scope;
        var action = context.RecoverabilityAction;

        if (action is ImmediateRetry)
        {
            // Retry scheduled - scope stays pending
            return;
        }

        if (action is DelayedRetry)
        {
            // Store action for dispatcher to preserve scope on delayed retry envelope
            transportTransaction.Set(action);
            return;
        }

        bool markTerminalFailure = false;
        if (action is MoveToError)
        {
            if (!settings.MoveToErrorQueueOnFailure)
            {
                // Change action before routing happens
                context.RecoverabilityAction = RecoverabilityAction.Discard("Inline execution suppressed error queue routing.");
            }

            markTerminalFailure = true;
        }
        else if (action is Discard)
        {
            markTerminalFailure = true;
        }

        await next(context).ConfigureAwait(false);

        if (markTerminalFailure)
        {
            scope.MarkTerminalFailure(context.Exception);
        }
    }
}