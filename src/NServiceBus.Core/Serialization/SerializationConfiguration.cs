namespace NServiceBus.Serialization
{
    using Settings;

    /// <summary>
    /// Enables the serializers to extend the config with their specific configuration methods
    /// </summary>
    public class SerializationConfiguration
    {
// ReSharper disable once NotAccessedField.Local
        internal SettingsHolder settings;

        internal SerializationConfiguration(SettingsHolder settings)
        {
            this.settings = settings;
        }
    }
}