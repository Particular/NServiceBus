namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;

    class LogErrorOnInvalidLicenseBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {
        public Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next, CancellationToken cancellationToken)
        {
            Log.Error("Your license has expired");

            return next(context);
        }

        static readonly ILog Log = LogManager.GetLogger<LicenseManager>();
    }
}