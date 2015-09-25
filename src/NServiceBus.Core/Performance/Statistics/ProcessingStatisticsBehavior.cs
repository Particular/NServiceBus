namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;

    class ProcessingStatisticsBehavior : PhysicalMessageProcessingStageBehavior
    {
        public override async Task Invoke(Context context, Func<Task> next)
        {
            var state = new State();

            string timeSentString;
            var headers = context.Message.Headers;

            if (headers.TryGetValue(Headers.TimeSent, out timeSentString))
            {
                state.TimeSent = DateTimeExtensions.ToUtcDateTime(timeSentString);
            }

            state.ProcessingStarted = DateTime.UtcNow;

            context.Set(state);
            try
            {
                await next().ConfigureAwait(false);
            }
            finally
            {
                state.ProcessingEnded = DateTime.UtcNow;
            }
        }

        public class State
        {
            public DateTime? TimeSent{ get; set; }
            public DateTime ProcessingStarted { get; set; }
            public DateTime ProcessingEnded { get; set; }
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base(WellKnownStep.ProcessingStatistics, typeof(ProcessingStatisticsBehavior), "Collects timing for ProcessingStarted and ProcessingEnded")
            {
                InsertAfterIfExists("InvokeAuditPipeline");
            }
        }
    }
}