namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;

    class AuditInvalidLicenseBehavior : Behavior<IAuditContext>
    {
        public override async Task Invoke(IAuditContext context, Func<Task> next)
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