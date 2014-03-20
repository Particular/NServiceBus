﻿namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using ObjectBuilder.Common;
    using ObjectBuilder.Common.Config;
    using Persistence.InMemory.SagaPersister;
    using Persistence.InMemory.SubscriptionStorage;
    using Persistence.InMemory.TimeoutPersister;
    using Persistence.Msmq.SubscriptionStorage;
    using Persistence.Raven.SagaPersister;
    using Persistence.Raven.SubscriptionStorage;
    using Persistence.Raven.TimeoutPersister;
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

        public static Configure DefineTimeoutPersister(this Configure config, string persister)
        {
            if (string.IsNullOrEmpty(persister))
            {
                persister = TimeoutPersisters.Default.Settings["TimeoutPersister"];
            }

            if (persister.Contains(typeof(InMemoryTimeoutPersistence).FullName))
            {
                return config.UseInMemoryTimeoutPersister();
            }

            if (persister.Contains(typeof(RavenTimeoutPersistence).FullName))
            {
                config.RavenPersistence(() => "url=http://localhost:8080");
                return config.UseRavenTimeoutPersister();
            }

            CallConfigureForPersister(config, persister);

            return config;
        }

        public static Configure DefineSagaPersister(this Configure config, string persister)
        {
            if (string.IsNullOrEmpty(persister))
            {
                persister = SagaPersisters.Default.Settings["SagaPersister"];
            }

            if (persister.Contains(typeof(InMemorySagaPersister).FullName))
            {
                return config.InMemorySagaPersister();
            }

            if (persister.Contains(typeof(RavenSagaPersister).FullName))
            {
                config.RavenPersistence(() => "url=http://localhost:8080");
                return config.RavenSagaPersister();
            }

            CallConfigureForPersister(config, persister);

            return config;
        }

        public static Configure DefineSubscriptionStorage(this Configure config, string persister)
        {
            if (string.IsNullOrEmpty(persister))
            {
                persister = SubscriptionPersisters.Default.Settings["SubscriptionStorage"];
            }

            if (persister.Contains(typeof(InMemorySubscriptionStorage).FullName))
            {
                return config.InMemorySubscriptionStorage();
            }

            if (persister.Contains(typeof(RavenSubscriptionStorage).FullName))
            {
                config.RavenPersistence(() => "url=http://localhost:8080");
                return config.RavenSubscriptionStorage();
            }

            if (persister.Contains(typeof(MsmqSubscriptionStorage).FullName))
            {
                return config.MsmqSubscriptionStorage();
            }

            CallConfigureForPersister(config, persister);

            return config;
        }

        static void CallConfigureForPersister(Configure config, string persister)
        {
            var type = Type.GetType(persister);

            var typeName = "Configure" + type.Name;

            var configurer = Activator.CreateInstance(Type.GetType(typeName));

            dynamic dc = configurer;

            dc.Configure(config);
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