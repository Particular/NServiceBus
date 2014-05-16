namespace NServiceBus.Licensing
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using log4net;
    using Pipeline;
    using Pipeline.Contexts;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class LicenseBehavior : IBehavior<IncomingContext>
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

        static ILog Log = LogManager.GetLogger(typeof(LicenseBehavior));
    }
}