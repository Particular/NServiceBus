namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;

    class LogErrorOnInvalidLicenseBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {
        public Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, CancellationToken, Task> next, CancellationToken token)
        {
            Log.Error("Your license has expired");

            return next(context, token);
        }

        static readonly ILog Log = LogManager.GetLogger<LicenseManager>();
    }
}