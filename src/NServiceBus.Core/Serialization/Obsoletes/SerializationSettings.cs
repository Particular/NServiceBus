namespace NServiceBus.Settings
{
    /// <summary>
    /// Settings related to message serialization
    /// </summary>
    public class SerializationSettings
    {
        public  Configure Config;

        public SerializationSettings(Configure config)
        {
            Config = config;
        }

        /// <summary>
        /// Tells the framework to always wrap out going messages as if there was multiple messages being sent
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "6.0",TreatAsErrorFromVersion = "5", Message = "In version 5 multi-message sends was removed. So Wrapping messages is no longer required. It only remains for compatibility with 3.0 endpoints.")]
        public SerializationSettings WrapSingleMessages()
        {
            Config.Settings.Set("SerializationSettings.WrapSingleMessages", true);

            return this;
        }

        /// <summary>
        /// Tells the framework to not wrap out going messages as if there was multiple messages being sent
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "6.0",TreatAsErrorFromVersion = "5", Message = "In version 5 multi-message sends was removed. So Wrapping messages is no longer required. It only remains for compatibility with 3.0 endpoints.")]
        public SerializationSettings DontWrapSingleMessages()
        {
            Config.Settings.Set("SerializationSettings.WrapSingleMessages", false);

            return this;
        }
    }
}