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
        /// See: <a href="http://docs.particular.net/nservicebus/how-to-specify-your-input-queue-name">Here</a> for more details.
        /// </summary>
        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Message = "See http://docs.particular.net/nservicebus/how-to-specify-your-input-queue-name for how to configure the queue name.")]
        public static Configure DefineLocalAddressNameFunc(this Configure config, Func<string> setLocalAddressNameFunc)
        {
            throw new NotImplementedException();
        }
    }
}

