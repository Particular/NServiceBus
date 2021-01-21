namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;

    class ProcessingStatisticsBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {
        public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, CancellationToken, Task> next, CancellationToken token)
        {
            var state = new State();

            var headers = context.Message.Headers;
            if (headers.TryGetValue(Headers.TimeSent, out var timeSentString))
            {
                state.TimeSent = DateTimeOffsetHelper.ToDateTimeOffset(timeSentString);
            }

            state.ProcessingStarted = DateTimeOffset.UtcNow;
            context.Extensions.Set(state);
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await next(context, token).ConfigureAwait(false);
            }
            finally
            {
                stopwatch.Stop();
                state.ProcessingEnded = state.ProcessingStarted + stopwatch.Elapsed;
            }
        }

        public class State
        {
            public DateTimeOffset? TimeSent { get; set; }
            public DateTimeOffset ProcessingStarted { get; set; }
            public DateTimeOffset ProcessingEnded { get; set; }
        }
    }
}
