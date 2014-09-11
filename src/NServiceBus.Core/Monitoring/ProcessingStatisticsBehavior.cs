namespace NServiceBus
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;

    class ProcessingStatisticsBehavior : IBehavior<IncomingContext>
    {
        public void Invoke(IncomingContext context, Action next)
        {
            string timeSentString;
            var headers = context.PhysicalMessage.Headers;

            if (headers.TryGetValue(Headers.TimeSent, out timeSentString))
            {
                context.Set("IncomingMessage.TimeSent", DateTimeExtensions.ToUtcDateTime(timeSentString));
            }

            context.Set("IncomingMessage.ProcessingStarted", DateTime.UtcNow);

            try
            {
                next();
            }
            finally
            {
                context.Set("IncomingMessage.ProcessingEnded", DateTime.UtcNow);
            }
        }
    }
}