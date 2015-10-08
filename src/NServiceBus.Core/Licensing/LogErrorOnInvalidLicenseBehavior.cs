namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;

    class LogErrorOnInvalidLicenseBehavior : Behavior<PhysicalMessageProcessingContext>
    {
        public override async Task Invoke(PhysicalMessageProcessingContext context, Func<Task> next)
        {
            Log.Error("Your license has expired");

            await next().ConfigureAwait(false);
        }

        static ILog Log = LogManager.GetLogger<LogErrorOnInvalidLicenseBehavior>();
    }
}