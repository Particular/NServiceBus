namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;

    class LogErrorOnInvalidLicenseBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            Log.Error("Your license has expired");

            return next();
        }

        static ILog Log = LogManager.GetLogger<LogErrorOnInvalidLicenseBehavior>();
    }
}