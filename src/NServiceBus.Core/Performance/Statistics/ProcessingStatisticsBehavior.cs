namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class ProcessingStatisticsBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {
        public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            var state = new State
            {
                ProcessingStarted = DateTimeOffset.UtcNow
            };

            context.Extensions.Set(state);
            await next(context).ConfigureAwait(false);
        }

        public class State
        {
            public DateTimeOffset ProcessingStarted { get; set; }
        }
    }
}
