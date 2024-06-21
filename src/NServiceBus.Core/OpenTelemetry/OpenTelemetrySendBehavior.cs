namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;

class OpenTelemetrySendBehavior : IBehavior<IOutgoingSendContext, IOutgoingSendContext>
{
    public Task Invoke(IOutgoingSendContext context, Func<IOutgoingSendContext, Task> next)
    {
        // sends never start a new trace on receive
        context.Headers[Headers.StartNewTrace] = bool.FalseString;

        // unless the user explicitly requests to start a new trace
        bool breakTraceWasSet = context.Extensions.TryGet<bool>(StartNewTraceOnReceive, out var breakTraceWasRequested);
        if (breakTraceWasSet && breakTraceWasRequested)
        {
            context.Headers[Headers.StartNewTrace] = bool.TrueString;
        }

        return next(context);
    }

    public const string StartNewTraceOnReceive = "BreakTraceRequested";
}