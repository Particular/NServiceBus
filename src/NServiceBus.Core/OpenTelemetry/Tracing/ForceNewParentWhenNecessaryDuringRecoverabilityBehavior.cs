namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;

class ForceNewParentWhenNecessaryDuringRecoverabilityBehavior : IBehavior<IRecoverabilityContext, IRecoverabilityContext>
{
    public Task Invoke(IRecoverabilityContext context, Func<IRecoverabilityContext, Task> next)
    {
        if (!context.Headers.ContainsKey(Headers.DiagnosticsTraceParent))
        {
            return next(context);
        }

        if (context.RecoverabilityAction is MoveToError or DelayedRetry)
        {
            // Setting it to the metadata makes sure it is propagated to the headers
            // even in more advanced scenarios like native dead-lettering
            context.Metadata[Headers.StartNewTrace] = bool.TrueString;
        }

        // DelayedRetry never uses fault metadata so we have to populate it manually to the header
        if (context.RecoverabilityAction is DelayedRetry)
        {
            context.Headers[Headers.StartNewTrace] = context.Metadata[Headers.StartNewTrace];
        }

        return next(context);
    }
}