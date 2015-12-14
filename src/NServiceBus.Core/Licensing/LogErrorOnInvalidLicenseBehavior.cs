namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;

    class LogErrorOnInvalidLicenseBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            Log.Error("Your license has expired");

            await next().ConfigureAwait(false);
        }

        static ILog Log = LogManager.GetLogger<LogErrorOnInvalidLicenseBehavior>();
    }
}