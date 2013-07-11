namespace NServiceBus
{
    using System.Security.Principal;
    using Unicast.Config;

    /// <summary>
    /// Contains extension methods for the purpose of configuring impersonation.
    /// </summary>
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
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [ObsoleteEx(Message = "This method was incorrectly named, there is no true impersonation, all this method does is populate the current handler thread with an IPrincipal, because of this we decided to rename it to RunHandlersUnderIncomingPrincipal()", RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0")]
        public static ConfigUnicastBus ImpersonateSender(this ConfigUnicastBus config, bool value)
        {
            return config.RunHandlersUnderIncomingPrincipal(value);
        }

        /// <summary>
        /// Instructs the bus to run the processing of messages being handled under the incoming user principal, by default this is a <see cref="GenericPrincipal"/> created from the <see cref="Headers.WindowsIdentityName"/>.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ConfigUnicastBus RunHandlersUnderIncomingPrincipal(this ConfigUnicastBus config, bool value)
        {
            Impersonate = value;

            return config;
        }

        public static bool Impersonate { get; private set; }
    }
}