namespace NServiceBus
{
    using System;

    class ProcessingStatisticsBehavior : PhysicalMessageProcessingStageBehavior
    {
        public override void Invoke(Context context, Action next)
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