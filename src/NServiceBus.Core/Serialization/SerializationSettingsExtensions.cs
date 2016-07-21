namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Serialization;
    using Settings;

    static class SerializationSettingsExtensions
    {
        const string AdditionalSerializersSettingsKey = "AdditionalDeserializers";
        const string MainSerializerSettingsKey = "MainSerializer";

        public static List<Tuple<SerializationDefinition, SettingsHolder>> GetAdditionalSerializers(this SettingsHolder settings)
        {
            List<Tuple<SerializationDefinition, SettingsHolder>> deserializers;
            if (!settings.TryGet(AdditionalSerializersSettingsKey, out deserializers))
            {
                deserializers = new List<Tuple<SerializationDefinition, SettingsHolder>>();
                settings.Set(AdditionalSerializersSettingsKey, deserializers);
            }
            return deserializers;
        }

        public static List<Tuple<SerializationDefinition, SettingsHolder>> GetAdditionalSerializers(this ReadOnlySettings settings)
        {
            List<Tuple<SerializationDefinition, SettingsHolder>> deserializers;
            if (settings.TryGet(AdditionalSerializersSettingsKey, out deserializers))
            {
                return deserializers;
            }
            return new List<Tuple<SerializationDefinition, SettingsHolder>>(0);
        }

        public static void SetMainSerializer(this SettingsHolder settings, SerializationDefinition definition, SettingsHolder serializerSpecificSettings)
        {
            settings.Set(MainSerializerSettingsKey, Tuple.Create(definition, serializerSpecificSettings));
        }

        public static Tuple<SerializationDefinition, SettingsHolder> GetMainSerializer(this ReadOnlySettings settings)
        {
            Tuple<SerializationDefinition, SettingsHolder> defaultSerializerAndSettings;
            if (!settings.TryGet(MainSerializerSettingsKey, out defaultSerializerAndSettings))
            {
                defaultSerializerAndSettings = Tuple.Create<SerializationDefinition, SettingsHolder>(new XmlSerializer(), new SettingsHolder());
            }
            return defaultSerializerAndSettings;
        }
    }
}