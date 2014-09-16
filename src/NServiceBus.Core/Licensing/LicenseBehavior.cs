namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using Logging;
    using NServiceBus.Licensing;
    using Pipeline;
    using Pipeline.Contexts;

    class LicenseBehavior : IBehavior<IncomingContext>
    {
        public bool LicenseExpired { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {
            context.PhysicalMessage.Headers[Headers.HasLicenseExpired] = LicenseExpired.ToString().ToLower();

            next();

            if (Debugger.IsAttached)
            {
                if (LicenseManager.HasLicenseExpired())
                {
                    Log.Error("Your license has expired");
                }
            }
        }

        static ILog Log = LogManager.GetLogger<LicenseBehavior>();
    }
}