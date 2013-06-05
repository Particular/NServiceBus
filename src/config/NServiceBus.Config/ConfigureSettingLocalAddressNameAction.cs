using System;

namespace NServiceBus.Config.Advanced
{
    /// <summary>
    /// Allow overriding local address name.
    /// </summary>
    public static class ConfigureSettingLocalAddressNameAction
    {
        /// <summary>
        /// By default local address should equal endpoint name.
        /// See: <a href="http://particular.net/articles/how-to-specify-your-input-queue-name">Here</a> for more details.
        /// </summary>
        private static Func<string> defineLocalAddressNameFunc = Configure.GetEndpointNameAction;

        /// <summary>
        /// Set a function that overrides the default naming of NServiceBus local addresses.
        /// See: <a href="http://particular.net/articles/how-to-specify-your-input-queue-name">Here</a> for more details.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="setLocalAddressNameFunc"></param>
        /// <returns></returns>
        public static Configure DefineLocalAddressNameFunc(this Configure config, Func<string> setLocalAddressNameFunc)
        {
            defineLocalAddressNameFunc = setLocalAddressNameFunc;
            return config;
        }
        /// <summary>
        /// Execute function that returns the NServiceBus local addresses name. If not override by the user, NServiceBus defaults will be used.
        /// See: <a href="http://particular.net/articles/how-to-specify-your-input-queue-name">Here</a> for more details.
        /// </summary>
        /// <returns></returns>
        public static string GetLocalAddressName()
        {
            return defineLocalAddressNameFunc();
        }
    }
}
