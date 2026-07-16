#nullable enable

namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;
using Transport;

class OpenTelemetryDelayedMessageBehavior(InstrumentationOptions instrumentationOptions) : IBehavior<IRoutingContext, IRoutingContext>
{
    public Task Invoke(IRoutingContext context, Func<IRoutingContext, Task> next)
    {
        // Recoverability already decided the trace boundary for delayed retries: the retried message
        // carries the decision in its headers, copied from recoverability metadata before the routing
        // context is created. The DelayedRetries header identifies that dispatch path — the StartNewTrace
        // header itself cannot be used because the send behavior stamps it on every outgoing message.
        if (context.Message.Headers.ContainsKey(Headers.DelayedRetries))
        {
            return next(context);
        }

        bool isDelayed = context.Extensions.TryGet<DispatchProperties>(out var dispatchProperties)
                         && (dispatchProperties.DelayDeliveryWith != null || dispatchProperties.DoNotDeliverBefore != null);

        if (!isDelayed)
        {
            return next(context);
        }

        // A per-message override was already resolved into the header by the send behavior.
        if (context.Extensions.TryGet(OpenTelemetryExtensions.TraceConnectorOverrideKey, out TraceMode _))
        {
            return next(context);
        }

        context.Extensions.Set(Headers.StartNewTrace,
            instrumentationOptions.DelayedSendTraceMode == TraceMode.StartNew ? bool.TrueString : bool.FalseString);

        return next(context);
    }
}
