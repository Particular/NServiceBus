namespace NServiceBus;

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
        if (!settings.TryGet(MainSerializerSettingsKey, out Tuple<SerializationDefinition, SettingsHolder> mainSerializerAndSettings))
        {
            throw new Exception($"A serializer has not been configured. Use 'EndpointConfiguration.{nameof(SerializationConfigExtensions.UseSerialization)}()' to specify a serializer.");
        }

        return mainSerializerAndSettings;
    }
}