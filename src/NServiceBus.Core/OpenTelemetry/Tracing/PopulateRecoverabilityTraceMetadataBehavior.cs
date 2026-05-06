namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;

class PopulateRecoverabilityTraceMetadataBehavior : IBehavior<IRecoverabilityContext, IRecoverabilityContext>
{
    public Task Invoke(IRecoverabilityContext context, Func<IRecoverabilityContext, Task> next)
    {
        if (!context.Headers.ContainsKey(Headers.DiagnosticsTraceParent))
        {
            return next(context);
        }

        // Setting it to the metadata makes sure it is propagated to the headers
        // even in more advanced scenarios like native dead-lettering
        context.Metadata[Headers.StartNewTrace] = bool.TrueString;

        return next(context);
    }
}