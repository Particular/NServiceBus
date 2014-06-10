namespace NServiceBus.Serialization
{
    using Settings;

    public class SerializationConfiguration
    {
// ReSharper disable once NotAccessedField.Local
        internal SettingsHolder settings;

        public SerializationConfiguration(SettingsHolder settings)
        {
            this.settings = settings;
        }
    }
}