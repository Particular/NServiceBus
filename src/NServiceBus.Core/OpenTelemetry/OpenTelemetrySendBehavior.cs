#nullable enable

namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;
using Transport;

class OpenTelemetrySendBehavior(InstrumentationOptions instrumentationOptions) : IBehavior<IOutgoingSendContext, IOutgoingSendContext>
{
    public Task Invoke(IOutgoingSendContext context, Func<IOutgoingSendContext, Task> next)
    {
        // the per-message override wins over the endpoint-level default
        var operationTraceMode = context.Extensions.TryGet(OpenTelemetryExtensions.TraceConnectorOverrideKey, out TraceMode requestedConnector)
            ? requestedConnector
            : instrumentationOptions.SendTraceMode;

        if (operationTraceMode != TraceMode.StartNew)
        {
            // This is needed to ensure the trace continuation behavior is backwards compatible.
            // If the message is delayed, we always start a new trace unless different behavior is explicitly configured.
            var isDelayed = context.Extensions.TryGet<DispatchProperties>(out var dispatchProperties) &&
                            (dispatchProperties.DelayDeliveryWith != null || dispatchProperties.DoNotDeliverBefore != null);

            if (isDelayed)
            {
                var isSagaTimeout = context.Headers.ContainsKey(Headers.IsSagaTimeoutMessage);
                operationTraceMode = isSagaTimeout
                    ? instrumentationOptions.DelayedDelivery.SagaTimeoutTraceMode
                    : instrumentationOptions.DelayedDelivery.SendOperationTraceMode;
            }
        }

        context.Headers[Headers.StartNewTrace] = TraceModeHeaderValue.From(operationTraceMode);

        return next(context);
    }
}