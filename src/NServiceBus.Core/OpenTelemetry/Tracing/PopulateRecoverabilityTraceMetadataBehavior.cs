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

        var connector = context.RecoverabilityAction switch
        {
            DelayedRetry => instrumentationOptions.DelayedRetryTraceMode,
            MoveToError => instrumentationOptions.ErrorMessageTraceMode,
            // custom recoverability actions keep the pre-existing behavior of always starting a new trace
            _ => TraceMode.StartNew
        };

        // Setting it to the metadata makes sure it is propagated to the headers
        // even in more advanced scenarios like native dead-lettering
        context.Metadata[Headers.StartNewTrace] = connector == TraceMode.StartNew ? bool.TrueString : bool.FalseString;

        return next(context);
    }
}
