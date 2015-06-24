namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using Logging;
    using NServiceBus.Audit;
    using Pipeline;

    class NotifyOnInvalidLicenseBehavior : Behavior<AuditContext>
    {
        public NotifyOnInvalidLicenseBehavior(bool licenseExpired)
        {
            this.licenseExpired = licenseExpired;
        }

        public override void Invoke(AuditContext context, Action next)
        {
            context.AddAuditData(Headers.HasLicenseExpired,licenseExpired.ToString().ToLower());

            next();

            if (licenseExpired && Debugger.IsAttached)
            {
                Log.Error("Your license has expired");
            }
        }

        static ILog Log = LogManager.GetLogger<NotifyOnInvalidLicenseBehavior>();
        bool licenseExpired;

        public class Registration : RegisterStep
        {
            public Registration()
                : base("LicenseReminder", typeof(NotifyOnInvalidLicenseBehavior), "Enforces the licensing policy")
            {
                InsertBeforeIfExists(WellKnownStep.AuditProcessedMessage);
            }
        }
    }
}