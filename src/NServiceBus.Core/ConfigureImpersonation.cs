namespace NServiceBus
{
    using System;
    using System.Security.Principal;
    using System.Threading;
    using Impersonation;
    using ObjectBuilder;
    using Unicast.Config;

    /// <summary>
    /// Contains extension methods for the purpose of configuring impersonation.
    /// </summary>
    [ObsoleteEx(
        Message = "The impersonation feature has been removed due to confusion of it being a security feature." +
                  "Once you stop using this feature the Thread.CurrentPrincipal will no longer be set to a fake principal containing the username. However you can still get access to that information using the message headers.", 
        Replacement = "message.GetHeader(Headers.WindowsIdentityName)",
        RemoveInVersion = "5.0", 
        TreatAsErrorFromVersion = "5.0")]
    public static class ConfigureImpersonation
    {
        /// <summary>
        /// Impersonate by default, otherwise this configuration would not be backward compatible
        /// </summary>
        static ConfigureImpersonation()
        {
            Impersonate = true;
        }

        /// <summary>
        /// Instructs the bus to run the processing of messages being handled
        /// under the permissions of the sender of the message.
        /// </summary>
        public static ConfigUnicastBus ImpersonateSender(this ConfigUnicastBus config, bool value)
        {
            return config.RunHandlersUnderIncomingPrincipal(value);
        }

        /// <summary>
        /// Instructs the bus to run the processing of messages being handled under the incoming user principal, by default this is a <see cref="GenericPrincipal"/> created from the <see cref="Headers.WindowsIdentityName"/>.
        /// </summary>
        public static ConfigUnicastBus RunHandlersUnderIncomingPrincipal(this ConfigUnicastBus config, bool value)
        {
            Impersonate = value;

            return config;
        }

        public static bool Impersonate { get; private set; }

        internal static void SetupImpersonation(IBuilder childBuilder, TransportMessage message)
        {
            if (!Impersonate)
                return;
            var impersonator = childBuilder.Build<ExtractIncomingPrincipal>();

            if (impersonator == null)
                throw new InvalidOperationException("Run handler under incoming principal is configured for this endpoint but no implementation of ExtractIncomingPrincipal has been found. Please register one.");

            var principal = impersonator.GetPrincipal(message);

            if (principal == null)
                return;

            Thread.CurrentPrincipal = principal;
        }
    }
}