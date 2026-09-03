namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;

class PopulateRecoverabilityTraceMetadataBehavior(InstrumentationOptions instrumentationOptions) : IBehavior<IRecoverabilityContext, IRecoverabilityContext>
{
    public Task Invoke(IRecoverabilityContext context, Func<IRecoverabilityContext, Task> next)
    {
        if (!context.Headers.ContainsKey(Headers.DiagnosticsTraceParent))
        {
            return next(context);
        }

        if (context.RecoverabilityAction is DelayedRetry)
        {
            // Setting it to the metadata makes sure it is propagated to the headers
            // even in more advanced scenarios like native dead-lettering
            context.Metadata[Headers.StartNewTrace] = TraceModeHeaderValue.From(instrumentationOptions.Recoverability.DelayedRetryTraceMode);
        }
        else if (context.RecoverabilityAction is MoveToError)
        {
            // Not currently configurable; preserves the pre-existing behavior of always starting a new trace.
            context.Metadata[Headers.StartNewTrace] = bool.TrueString;
        }

        return next(context);
    }
}