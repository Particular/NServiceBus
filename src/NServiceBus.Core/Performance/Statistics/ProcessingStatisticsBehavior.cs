namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Pipeline;

    class ProcessingStatisticsBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {
        public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            var state = new State();

            string timeSentString;
            var headers = context.Message.Headers;

            if (headers.TryGetValue(Headers.TimeSent, out timeSentString))
            {
                state.TimeSent = DateTimeExtensions.ToUtcDateTime(timeSentString);
            }

            state.ProcessingStarted = DateTime.UtcNow;
            context.Extensions.Set(state);
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await next(context).ConfigureAwait(false);
            }
            finally
            {
                stopwatch.Stop();
                state.ProcessingEnded = state.ProcessingStarted + stopwatch.Elapsed;
            }
        }

        public class State
        {
            public DateTime? TimeSent { get; set; }
            public DateTime ProcessingStarted { get; set; }
            public DateTime ProcessingEnded { get; set; }
        }
    }
}
