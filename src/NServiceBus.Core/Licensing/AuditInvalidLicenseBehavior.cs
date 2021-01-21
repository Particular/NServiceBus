namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;

    class AuditInvalidLicenseBehavior : IBehavior<IAuditContext, IAuditContext>
    {
        public async Task Invoke(IAuditContext context, Func<IAuditContext, CancellationToken, Task> next, CancellationToken token)
        {
            context.AddAuditData(Headers.HasLicenseExpired, "true");

            await next(context, token).ConfigureAwait(false);

            if (Debugger.IsAttached)
            {
                Log.Error("Your license has expired");
            }
        }

        static readonly ILog Log = LogManager.GetLogger<AuditInvalidLicenseBehavior>();
    }
}