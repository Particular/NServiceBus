namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Logging;
    using Serialization;
    using Settings;

    static class SerializationSettingsExtensions
    {
        const string AdditionalSerializersSettingsKey = "AdditionalDeserializers";
        const string MainSerializerSettingsKey = "MainSerializer";
        static readonly ILog log = LogManager.GetLogger(nameof(SerializationSettingsExtensions));

        public static List<Tuple<SerializationDefinition, SettingsHolder>> GetAdditionalSerializers(this SettingsHolder settings)
        {
            if (!settings.TryGet(AdditionalSerializersSettingsKey, out List<Tuple<SerializationDefinition, SettingsHolder>> deserializers))
            {
                deserializers = [];
                settings.Set(AdditionalSerializersSettingsKey, deserializers);
            }
            return deserializers;
        }

        public static List<Tuple<SerializationDefinition, SettingsHolder>> GetAdditionalSerializers(this IReadOnlySettings settings)
        {
            if (settings.TryGet(AdditionalSerializersSettingsKey, out List<Tuple<SerializationDefinition, SettingsHolder>> deserializers))
            {
                return deserializers;
            }
            return new List<Tuple<SerializationDefinition, SettingsHolder>>(0);
        }

        public static void SetMainSerializer(this SettingsHolder settings, SerializationDefinition definition, SettingsHolder serializerSpecificSettings)
        {
            settings.Set(MainSerializerSettingsKey, Tuple.Create(definition, serializerSpecificSettings));
        }

        public static Tuple<SerializationDefinition, SettingsHolder> GetMainSerializer(this IReadOnlySettings settings)
        {
            if (!settings.TryGet(MainSerializerSettingsKey, out Tuple<SerializationDefinition, SettingsHolder> defaultSerializerAndSettings))
            {
                var noDefaultSerializerMsg = $"Because no message serializer was selected, the default {nameof(XmlSerializer)} will be used instead. In a future version of NServiceBus the {nameof(XmlSerializer)} will no longer be the default. For better forward compatibility, either choose a different message serializer, or make the choice of XML serialization explicit using endpointConfiguration.{nameof(SerializationConfigExtensions.UseSerialization)}<{nameof(XmlSerializer)}>()";
                log.Warn(noDefaultSerializerMsg);
                defaultSerializerAndSettings = Tuple.Create<SerializationDefinition, SettingsHolder>(new XmlSerializer(), new SettingsHolder());
            }
            return defaultSerializerAndSettings;
        }
    }
}