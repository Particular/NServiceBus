#nullable enable

namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;

class OpenTelemetrySendBehavior(InstrumentationOptions instrumentationOptions) : IBehavior<IOutgoingSendContext, IOutgoingSendContext>
{
    public Task Invoke(IOutgoingSendContext context, Func<IOutgoingSendContext, Task> next)
    {
        // the per-message override wins over the endpoint-level default
        var connector = context.Extensions.TryGet(OpenTelemetryExtensions.TraceConnectorOverrideKey, out TraceMode requestedConnector)
            ? requestedConnector
            : instrumentationOptions.SendTraceMode;

        context.Headers[Headers.StartNewTrace] = connector == TraceMode.StartNew ? bool.TrueString : bool.FalseString;

        return next(context);
    }
}