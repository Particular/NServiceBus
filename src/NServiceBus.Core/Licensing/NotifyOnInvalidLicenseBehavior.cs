namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Audit;
    using Logging;
    using Pipeline;

    class NotifyOnInvalidLicenseBehavior : Behavior<AuditContext>
    {
        public NotifyOnInvalidLicenseBehavior(bool licenseExpired)
        {
            this.licenseExpired = licenseExpired;
        }

        public override async Task Invoke(AuditContext context, Func<Task> next)
        {
            context.AddAuditData(Headers.HasLicenseExpired, licenseExpired.ToString().ToLower());

            await next().ConfigureAwait(false);

            if (licenseExpired && Debugger.IsAttached)
            {
                Log.Error("Your license has expired");
            }
        }

        bool licenseExpired;

        public class Registration : RegisterStep
        {
            public Registration()
                : base("LicenseReminder", typeof(NotifyOnInvalidLicenseBehavior), "Enforces the licensing policy")
            {
                InsertBeforeIfExists(WellKnownStep.AuditProcessedMessage);
            }
        }

        static ILog Log = LogManager.GetLogger<NotifyOnInvalidLicenseBehavior>();
    }
}