namespace NServiceBus.Config.Advanced
{
    using System;

    /// <summary>
    /// Allow overriding local address name.
    /// </summary>
    [ObsoleteEx(Message = "Moved to NServiceBus namespace.", RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0")]
    public static class ConfigureSettingLocalAddressNameAction
    {
        /// <summary>
        /// Set a function that overrides the default naming of NServiceBus local addresses.
        /// See: <a href="http://particular.net/articles/how-to-specify-your-input-queue-name">Here</a> for more details.
        /// </summary>
        [ObsoleteEx(Message = "Moved to NServiceBus namespace.", RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0")]
        public static Configure DefineLocalAddressNameFunc(this Configure config, Func<string> setLocalAddressNameFunc)
        {
            NServiceBus.ConfigureSettingLocalAddressNameAction.defineLocalAddressNameFunc = setLocalAddressNameFunc;
            return config;
        }
    }
}

namespace NServiceBus
{
    using System;

    /// <summary>
    /// Allow overriding local address name.
    /// </summary>
    public static class ConfigureSettingLocalAddressNameAction
    {
        internal static Func<string> defineLocalAddressNameFunc = Configure.GetEndpointNameAction;

        /// <summary>
        /// Set a function that overrides the default naming of NServiceBus local addresses.
        /// See: <a href="http://particular.net/articles/how-to-specify-your-input-queue-name">Here</a> for more details.
        /// </summary>
        public static Configure DefineLocalAddressNameFunc(this Configure config, Func<string> setLocalAddressNameFunc)
        {
            defineLocalAddressNameFunc = setLocalAddressNameFunc;
            return config;
        }
        /// <summary>
        /// Execute function that returns the NServiceBus local addresses name. If not override by the user, NServiceBus defaults will be used.
        /// See: <a href="http://particular.net/articles/how-to-specify-your-input-queue-name">Here</a> for more details.
        /// </summary>
        internal static string GetLocalAddressName()
        {
            return defineLocalAddressNameFunc();
        }
    }
}

