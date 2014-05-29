namespace NServiceBus
{
    using System;

    /// <summary>
    /// Allow overriding local address name.
    /// </summary>
    public static class ConfigureSettingLocalAddressNameAction
    {
        static Func<string> defineLocalAddressNameFunc = () => Configure.Instance.Settings.EndpointName();

        /// <summary>
        /// Set a function that overrides the default naming of NServiceBus local addresses.
        /// See: <a href="http://docs.particular.net/nservicebus/how-to-specify-your-input-queue-name">Here</a> for more details.
        /// </summary>
        public static Configure DefineLocalAddressNameFunc(this Configure config, Func<string> setLocalAddressNameFunc)
        {
            defineLocalAddressNameFunc = setLocalAddressNameFunc;
            return config;
        }
        /// <summary>
        /// Execute function that returns the NServiceBus local addresses name. If not override by the user, NServiceBus defaults will be used.
        /// See: <a href="http://docs.particular.net/nservicebus/how-to-specify-your-input-queue-name">Here</a> for more details.
        /// </summary>
        internal static string GetLocalAddressName()
        {
            return defineLocalAddressNameFunc();
        }
    }
}

