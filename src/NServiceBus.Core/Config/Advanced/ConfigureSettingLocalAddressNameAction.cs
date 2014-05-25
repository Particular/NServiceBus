namespace NServiceBus
{
    using System;

    /// <summary>
    /// Allow overriding local address name.
    /// </summary>
    public static class ConfigureSettingLocalAddressNameAction
    {

        /// <summary>
        /// Set a function that overrides the default naming of NServiceBus local addresses.
        /// See: <a href="http://particular.net/articles/how-to-specify-your-input-queue-name">Here</a> for more details.
        /// </summary>
        public static Configure DefineLocalAddressNameFunc(this Configure config, Func<string> setLocalAddressNameFunc)
        {
            config.defineLocalAddressNameFunc = setLocalAddressNameFunc;
            return config;
        }
        /// <summary>
        /// Execute function that returns the NServiceBus local addresses name. If not override by the user, NServiceBus defaults will be used.
        /// See: <a href="http://particular.net/articles/how-to-specify-your-input-queue-name">Here</a> for more details.
        /// </summary>
        internal static string GetLocalAddressName(this Configure config)
        {
            return config.defineLocalAddressNameFunc();
        }
    }
}

