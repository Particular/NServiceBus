namespace NServiceBus.Settings
{
    /// <summary>
    /// Settings related to message serialization
    /// </summary>
    public class SerializationSettings
    {
        /// <summary>
        /// Tells the framework to always wrap out going messages as if there was multiple messages beeing sent
        /// </summary>
        /// <returns></returns>
        public SerializationSettings WrapSingleMessages()
        {
            SettingsHolder.Set("SerializationSettings.WrapSingleMessages",true);

            return this;
        }
    }
}