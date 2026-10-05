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
        var operationTraceMode = context.Extensions.TryGetTraceModeOverride(out var requestedConnector)
            ? requestedConnector
            : instrumentationOptions.SendTraceMode;

        if (operationTraceMode == TraceMode.StartNew)
        {
            context.Headers[Headers.StartNewTrace] = bool.TrueString;
        }
        else
        {
            // This is needed to ensure the trace continuation behavior is backwards compatible.
            // If the message is delayed, we always start a new trace unless different behavior is explicitly configured. 
            var isDelayed = context.Extensions.TryGet<DispatchProperties>(out var dispatchProperties) &&
                            (dispatchProperties.DelayDeliveryWith != null || dispatchProperties.DoNotDeliverBefore != null);

            bool startNewTrace;
            if (isDelayed)
            {
                var isSagaTimeout = context.Headers.ContainsKey(Headers.IsSagaTimeoutMessage);
                var mode = isSagaTimeout
                    ? instrumentationOptions.DelayedDelivery.SagaTimeoutTraceMode
                    : instrumentationOptions.DelayedDelivery.SendOperationTraceMode;
                startNewTrace = mode == TraceMode.StartNew;
            }
            else
            {
                startNewTrace = false;
            }

            // An absent header means "continue the trace" on receive, so the header is only put on the wire when a
            // new trace is requested. Remove rather than skip: a caller may have copied it from an incoming message.
            if (startNewTrace)
            {
                context.Headers[Headers.StartNewTrace] = bool.TrueString;
            }
            else
            {
                context.Headers.Remove(Headers.StartNewTrace);
            }
        }

        return next(context);
    }
}