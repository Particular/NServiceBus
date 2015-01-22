namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using Logging;
    using Pipeline;

    class NotifyOnInvalidLicenseBehavior : PhysicalMessageProcessingStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            context.PhysicalMessage.Headers[Headers.HasLicenseExpired] = true.ToString().ToLower();

            next();

            if (Debugger.IsAttached)
            {
                Log.Error("Your license has expired");
            }
        }

        static ILog Log = LogManager.GetLogger<NotifyOnInvalidLicenseBehavior>();

        public class Registration : RegisterStep
        {
            public Registration()
                : base("LicenseReminder", typeof(NotifyOnInvalidLicenseBehavior), "Enforces the licensing policy")
            {
                InsertBefore(WellKnownStep.AuditProcessedMessage);
            }
        }
    }
}