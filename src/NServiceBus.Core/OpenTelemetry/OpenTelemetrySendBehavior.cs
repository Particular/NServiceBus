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
        var connector = context.Extensions.TryGet(OpenTelemetryExtensions.TraceConnectorOverrideKey, out TraceMode requestedConnector)
            ? requestedConnector
            : instrumentationOptions.SendTraceMode;

        context.Headers[Headers.StartNewTrace] = connector == TraceMode.StartNew ? bool.TrueString : bool.FalseString;
        // if the user explicitly requests to start a new trace, this always wins
        bool startNewTraceOnReceiveWasSet = context.Extensions.TryGet<bool>(StartNewTraceOnReceive, out var startNewTraceOnReceiveWasRequested);
        if (startNewTraceOnReceiveWasSet && startNewTraceOnReceiveWasRequested)
        {
            context.Headers[Headers.StartNewTrace] = bool.TrueString;
        }
        else
        {
            var isDelayed = context.Extensions.TryGet<DispatchProperties>(out var dispatchProperties) &&
                            (dispatchProperties.DelayDeliveryWith != null || dispatchProperties.DoNotDeliverBefore != null);

            bool startNewTrace;
            if (isDelayed)
            {
                var isSagaTimeout = context.Headers.ContainsKey(Headers.IsSagaTimeoutMessage);
                var mode = isSagaTimeout
                    ? instrumentationOptions.DelayedDelivery.SagaTimeoutTraceMode
                    : instrumentationOptions.DelayedDelivery.SendOperationTraceMode;
                startNewTrace = mode == RecoverabilityTraceMode.StartNew;
            }
            else
            {
                startNewTrace = false;
            }

            context.Headers[Headers.StartNewTrace] = startNewTrace ? bool.TrueString : bool.FalseString;
        }

        return next(context);
    }
}