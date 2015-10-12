﻿namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using NServiceBus.Configuration.AdvanceExtensibility;
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

        public static async Task DefineTransport(this BusConfiguration config, IDictionary<string, string> settings, Type endpointBuilderType)
        {
            if (!settings.ContainsKey("Transport"))
            {
                settings = Transports.Default.Settings;
            }

            const string typeName = "ConfigureTransport";

            var transportType = Type.GetType(settings["Transport"]);
            var transportTypeName = "Configure" + transportType.Name;

            var configurerType = endpointBuilderType.GetNestedType(typeName) ??
                                 Type.GetType(transportTypeName, false);

            if (configurerType != null)
            {
                var configurer = Activator.CreateInstance(configurerType);

                dynamic dc = configurer;

                await dc.Configure(config);
                var cleanupMethod = configurer.GetType().GetMethod("Cleanup", BindingFlags.Public | BindingFlags.Instance);
                config.GetSettings().Set("CleanupTransport", cleanupMethod != null ? configurer : new Cleaner());
                return;
            }

            config.UseTransport(transportType).ConnectionString(settings["Transport.ConnectionString"]);
        }

        public static void DefineTransactions(this BusConfiguration config, IDictionary<string, string> settings)
        {
            if (settings.ContainsKey("Transactions.Disable"))
            {
                config.Transactions().Disable();
            }
        }

        public static async Task DefinePersistence(this BusConfiguration config, IDictionary<string, string> settings)
        {
            if (!settings.ContainsKey("Persistence"))
            { 
                settings = Persistence.Default.Settings;
            }

            var persistenceType = Type.GetType(settings["Persistence"]);


            var typeName = "Configure" + persistenceType.Name;

            var configurerType = Type.GetType(typeName, false);

            if (configurerType != null)
            {
                var configurer = Activator.CreateInstance(configurerType);

                dynamic dc = configurer;

                await dc.Configure(config);

                var cleanupMethod = configurer.GetType().GetMethod("Cleanup", BindingFlags.Public | BindingFlags.Instance);
                config.GetSettings().Set("CleanupPersistence", cleanupMethod != null ? configurer : new Cleaner());
                return;
            }

            config.UsePersistence(persistenceType);
        }

        class Cleaner
        {
            public Task Cleanup()
            {
                return Task.FromResult(0);
            }
        }

        public static void DefineBuilder(this BusConfiguration config, IDictionary<string, string> settings)
        {
            if (!settings.ContainsKey("Builder"))
            {
                var builderDescriptor = Builders.Default;

                if (builderDescriptor == null)
                {
                    return; //go with the default builder
                }

                settings = builderDescriptor.Settings;
            }

            var builderType = Type.GetType(settings["Builder"]);


            var typeName = "Configure" + builderType.Name;

            var configurerType = Type.GetType(typeName, false);

            if (configurerType != null)
            {
                var configurer = Activator.CreateInstance(configurerType);

                dynamic dc = configurer;

                dc.Configure(config);
            }

            config.UseContainer(builderType);
        }
    }
}