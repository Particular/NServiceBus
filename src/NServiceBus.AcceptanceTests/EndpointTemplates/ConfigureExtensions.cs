namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using AcceptanceTesting;
    using ObjectBuilder.Autofac;
    using ObjectBuilder.CastleWindsor;
    using ObjectBuilder.Common;
    using ObjectBuilder.Common.Config;
    using ObjectBuilder.Ninject;
    using ObjectBuilder.StructureMap;
    using ObjectBuilder.Unity;
    using Serializers.Binary;
    using Serializers.Json;
    using Serializers.XML;
    using Persistence.InMemory.SagaPersister;
    using Persistence.InMemory.SubscriptionStorage;
    using Persistence.Msmq.SubscriptionStorage;
    using Persistence.Raven.SagaPersister;
    using Persistence.Raven.SubscriptionStorage;

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

        public static Configure DefineHowManySubscriptionMessagesToWaitFor(this Configure config, int numberOfSubscriptionsToWaitFor)
        {
            config.Configurer.ConfigureProperty<EndpointConfigurationBuilder.SubscriptionsSpy>(
                    spy => spy.NumberOfSubscriptionsToWaitFor, numberOfSubscriptionsToWaitFor);

            return config;
        }

        public static Configure DefineTransport(this Configure config, IDictionary<string, string> settings)
        {
            if (!settings.ContainsKey("Transport"))
                settings = ScenarioDescriptors.Transports.Default.Settings;

            var transportType = Type.GetType(settings["Transport"]);

            return config.UseTransport(transportType, () => settings["Transport.ConnectionString"]);

        }

        public static Configure DefineSerializer(this Configure config, string serializer)
        {
            if (string.IsNullOrEmpty(serializer))
                return config;//xml is the default

            var type = Type.GetType(serializer);

            if (type == typeof (XmlMessageSerializer))
            {
                Configure.Serialization.Xml();
                return config;
            }


            if (type == typeof (JsonMessageSerializer))
            {
                Configure.Serialization.Json();
                return config;
            }

            if (type == typeof(BsonMessageSerializer))
            {
                Configure.Serialization.Bson();
                return config;
            }

            if (type == typeof (BinaryMessageSerializer))
            {
                Configure.Serialization.Binary();
                return config;
            }

            throw new InvalidOperationException("Unknown serializer:" + serializer);
        }


        public static Configure DefineSagaPersister(this Configure config, string persister)
        {
            if (string.IsNullOrEmpty(persister))
                return config.InMemorySagaPersister();

            var type = Type.GetType(persister);

            if (type == typeof(InMemorySagaPersister))
                return config.InMemorySagaPersister();

            if (type == typeof(RavenSagaPersister))
            {
                config.RavenPersistence(() => "url=http://localhost:8080");
                return config.RavenSagaPersister();

            }

            throw new InvalidOperationException("Unknown persister:" + persister);
        }


        public static Configure DefineSubscriptionStorage(this Configure config, string persister)
        {
            if (string.IsNullOrEmpty(persister))
                return config.InMemorySubscriptionStorage();

            var type = Type.GetType(persister);

            if (type == typeof(InMemorySubscriptionStorage))
                return config.InMemorySubscriptionStorage();

            if (type == typeof(RavenSubscriptionStorage))
            {
                config.RavenPersistence(() => "url=http://localhost:8080");
                return config.RavenSubscriptionStorage();

            }

          
            if (type == typeof(MsmqSubscriptionStorage))
            {
                return config.MsmqSubscriptionStorage();
            }

            throw new InvalidOperationException("Unknown persister:" + persister);
        }

        public static Configure DefineBuilder(this Configure config, string builder)
        {
            if (string.IsNullOrEmpty(builder))
                return config.DefaultBuilder();


            var container = (IContainer)Activator.CreateInstance(Type.GetType(builder));

            ConfigureCommon.With(config, container);

            return config;
        }
    }
}