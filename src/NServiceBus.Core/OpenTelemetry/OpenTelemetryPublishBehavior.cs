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
        var connector = context.Extensions.TryGet(OpenTelemetryExtensions.TraceConnectorOverrideKey, out TraceConnector requestedConnector)
            ? requestedConnector
            : instrumentationOptions.PublishedMessageTraceConnector;

        context.Headers[Headers.StartNewTrace] = connector == TraceConnector.SpanLink ? bool.TrueString : bool.FalseString;

        return next(context);
    }
}