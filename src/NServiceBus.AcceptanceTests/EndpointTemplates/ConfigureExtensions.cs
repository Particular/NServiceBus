namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using ObjectBuilder.Common;
    using ObjectBuilder.Common.Config;
    using Persistence;
    using ScenarioDescriptors;

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

            return config.UseTransport(transportType, c => c.ConnectionString(settings["Transport.ConnectionString"]));
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