namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;

class PopulateRecoverabilityTraceMetadataBehavior(InstrumentationOptions instrumentationOptions) : IBehavior<IRecoverabilityContext, IRecoverabilityContext>
{
    readonly InstrumentationOptions instrumentationOptions = instrumentationOptions;

    public Task Invoke(IRecoverabilityContext context, Func<IRecoverabilityContext, Task> next)
    {
        if (!context.Headers.ContainsKey(Headers.DiagnosticsTraceParent))
        {
            return next(context);
        }

        // Setting it to the metadata makes sure it is propagated to the headers
        // even in more advanced scenarios like native dead-lettering
        context.Metadata[Headers.StartNewTrace] = instrumentationOptions.Recoverability.DelayedRetryTraceMode == RecoverabilityTraceMode.StartNew
            ? bool.TrueString
            : bool.FalseString;

        return next(context);
    }
}