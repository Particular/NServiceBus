#nullable enable

namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;

class OpenTelemetryPublishBehavior(InstrumentationOptions instrumentationOptions) : IBehavior<IOutgoingPublishContext, IOutgoingPublishContext>
{
    public Task Invoke(IOutgoingPublishContext context, Func<IOutgoingPublishContext, Task> next)
    {
        // the per-message override wins over the endpoint-level default
        var connector = context.Extensions.TryGet(OpenTelemetryExtensions.TraceConnectorOverrideKey, out TraceMode requestedConnector)
            ? requestedConnector
            : instrumentationOptions.PublishTraceMode;

        // An absent header means "continue the trace" on receive, so the header is only put on the wire when a
        // new trace is requested. Remove rather than skip: a caller may have copied it from an incoming message.
        if (connector == TraceMode.StartNew)
        {
            context.Headers[Headers.StartNewTrace] = bool.TrueString;
        }
        else
        {
            context.Headers.Remove(Headers.StartNewTrace);
        }

        return next(context);
    }
}