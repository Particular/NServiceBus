namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
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

        public static void DefineTransport(this ConfigurationBuilder builder, IDictionary<string, string> settings)
        {
            if (!settings.ContainsKey("Transport"))
            {
                settings = Transports.Default.Settings;
            }

            var transportType = Type.GetType(settings["Transport"]);

            builder.UseTransport(transportType).ConnectionString(settings["Transport.ConnectionString"]);
        }

        public static void DefinePersistence(this ConfigurationBuilder config, IDictionary<string, string> settings)
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

            config.UsePersistence(persistenceType);
        }
    }
}