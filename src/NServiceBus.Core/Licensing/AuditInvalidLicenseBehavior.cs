namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Logging;
    using Audit;
    using Pipeline;

    class AuditInvalidLicenseBehavior : Behavior<AuditContext>
    {
        public override async Task Invoke(AuditContext context, Func<Task> next)
        {
            context.AddAuditData(Headers.HasLicenseExpired, true.ToString().ToLower());

            await next().ConfigureAwait(false);

            if (Debugger.IsAttached)
            {
                Log.Error("Your license has expired");
            }
        }

        static ILog Log = LogManager.GetLogger<AuditInvalidLicenseBehavior>();
    }
}