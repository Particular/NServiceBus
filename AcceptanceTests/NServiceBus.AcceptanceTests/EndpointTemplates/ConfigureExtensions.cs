﻿namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using AcceptanceTesting;
    using NServiceBus.ObjectBuilder.Autofac;
    using NServiceBus.ObjectBuilder.CastleWindsor;
    using NServiceBus.ObjectBuilder.Common.Config;
    using NServiceBus.ObjectBuilder.Ninject;
    using NServiceBus.ObjectBuilder.Spring;
    using NServiceBus.ObjectBuilder.StructureMap;
    using NServiceBus.ObjectBuilder.Unity;
    using NServiceBus.Serializers.Binary;
    using NServiceBus.Serializers.Json;
    using NServiceBus.Serializers.XML;

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

        public static Configure DefineTransport(this Configure config, string transport)
        {
            if (string.IsNullOrEmpty(transport))
            {
                return config.UseTransport<Msmq>();
            }

            var transportType = Type.GetType(transport);

            if (DefaultConnectionStrings.ContainsKey(transportType))
            {
                return config.UseTransport(transportType, () => DefaultConnectionStrings[transportType]);
            }

            return config.UseTransport(transportType);
        }

        public static Configure DefineSerializer(this Configure config, string serializer)
        {
            if (string.IsNullOrEmpty(serializer))
                return config.XmlSerializer();

            var type = Type.GetType(serializer);

            if (type == typeof (XmlMessageSerializer))
                return config.XmlSerializer();


            if (type == typeof (JsonMessageSerializer))
                return config.JsonSerializer();


            if (type == typeof (BsonMessageSerializer))
                return config.BsonSerializer();

            if (type == typeof (MessageSerializer))
                return config.BinarySerializer();


            throw new InvalidOperationException("Unknown serializer:" + serializer);
        }

        public static Configure DefineBuilder(this Configure config, string builder)
        {
            if (string.IsNullOrEmpty(builder))
                return config.DefaultBuilder();

            var type = Type.GetType(builder);

            if (type == typeof (AutofacObjectBuilder))
            {
                ConfigureCommon.With(config, new AutofacObjectBuilder(null));

                return config;
            }

            if (type == typeof (WindsorObjectBuilder))
                return config.CastleWindsorBuilder();

            if (type == typeof (NinjectObjectBuilder))
                return config.NinjectBuilder();

            if (type == typeof (SpringObjectBuilder))
                return config.SpringFrameworkBuilder();

            if (type == typeof (StructureMapObjectBuilder))
                return config.StructureMapBuilder();

            if (type == typeof (UnityObjectBuilder))
                return config.StructureMapBuilder();


            throw new InvalidOperationException("Unknown builder:" + builder);
        }

        private static readonly Dictionary<Type, string> DefaultConnectionStrings = new Dictionary<Type, string>
            {
                {typeof (RabbitMQ), "host=localhost"},
                {typeof (SqlServer), @"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;"},
                {typeof (ActiveMQ), @"Uri = activemq:tcp://localhost:61616"},
                {typeof (Msmq), @"cacheSendConnection=false;journal=false;"},
            };
    }
}