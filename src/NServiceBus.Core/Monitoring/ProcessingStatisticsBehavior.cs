namespace NServiceBus
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;

    class ProcessingStatisticsBehavior : IBehavior<IncomingContext>
    {
        public void Invoke(IncomingContext context, Action next)
        {
            //since the audit captures the physical message headers then lets place them there
            var headers = context.PhysicalMessage.Headers;
            headers[Headers.ProcessingStarted] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
            try
            {
                next();
            }
            finally
            {
                headers[Headers.ProcessingEnded] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
            }
        }
    }
}