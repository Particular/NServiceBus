namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using ObjectBuilder.Common;
    using ObjectBuilder.Common.Config;
    using Persistence;
    using ScenarioDescriptors;
    using Serializers.Binary;
    using Serializers.Json;
    using Serializers.XML;

    public static class ConfigureExtensions
    {
        public static string GetOrNull(this IDictionary<string, string> dictionary, string key)
        {
            if (!dictionary.ContainsKey(key))
            {
                return null;
            }

            return dictionary[key];
        }

        public static Configure DefineTransport(this Configure config, IDictionary<string, string> settings)
        {
            if (!settings.ContainsKey("Transport"))
            {
                settings = Transports.Default.Settings;
            }

            var transportType = Type.GetType(settings["Transport"]);

            return config.UseTransport(transportType, () => settings["Transport.ConnectionString"]);
        }

        public static Configure DefinePersistence(this Configure config, IDictionary<string, string> settings)
        {
            if (!settings.ContainsKey("Persistence"))
            {
                settings = Persistence.Default.Settings;
            }

            var persistenceType = Type.GetType(settings["Persistence"]);


            var typeName = "Configure" + persistenceType.Name + "Persistence";

            var configurerType = Type.GetType(typeName, false);

            if (configurerType != null)
            {
                var configurer = Activator.CreateInstance(configurerType);

                dynamic dc = configurer;

                dc.Configure(config);
            }

            return config.UsePersistence(persistenceType);
        }

        public static Configure DefineSerializer(this Configure config, string serializer)
        {
            if (string.IsNullOrEmpty(serializer))
            {
                return config; //xml is the default
            }

            var type = Type.GetType(serializer);

            if (type == typeof(XmlMessageSerializer))
            {
                Configure.Serialization.Xml();
                return config;
            }

            if (type == typeof(JsonMessageSerializer))
            {
                Configure.Serialization.Json();
                return config;
            }

            if (type == typeof(BsonMessageSerializer))
            {
                Configure.Serialization.Bson();
                return config;
            }

            if (type == typeof(BinaryMessageSerializer))
            {
                Configure.Serialization.Binary();
                return config;
            }

            throw new InvalidOperationException("Unknown serializer:" + serializer);
        }


        public static Configure DefineBuilder(this Configure config, string builder)
        {
            if (string.IsNullOrEmpty(builder))
            {
                return config.DefaultBuilder();
            }

            var container = (IContainer) Activator.CreateInstance(Type.GetType(builder));

            ConfigureCommon.With(config, container);

            return config;
        }
    }
}