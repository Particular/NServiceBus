#nullable enable

namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;

class OpenTelemetryPublishBehavior : IBehavior<IOutgoingPublishContext, IOutgoingPublishContext>
{
    public Task Invoke(IOutgoingPublishContext context, Func<IOutgoingPublishContext, Task> next)
    {
        // publishes always start a new trace on receive
        context.Headers[Headers.StartNewTrace] = bool.TrueString;

        // unless the user explicitly requests to continue the trace
        bool continueTraceWasSet = context.Extensions.TryGet<bool>(ContinueTraceOnReceive, out var continueTraceRequested);
        if (continueTraceWasSet && continueTraceRequested)
        {
            context.Headers[Headers.StartNewTrace] = bool.FalseString;
        }

        return next(context);
    }

    public const string ContinueTraceOnReceive = "ContinueTraceRequested";
}