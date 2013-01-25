namespace NServiceBus.IntegrationTests.Automated.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using Autofac;
    using Autofac.Core;
    using Autofac.Core.Lifetime;
    using ObjectBuilder.Autofac;
    using ObjectBuilder.CastleWindsor;
    using ObjectBuilder.Common.Config;
    using ObjectBuilder.Ninject;
    using ObjectBuilder.Spring;
    using ObjectBuilder.StructureMap;
    using ObjectBuilder.Unity;
    using Serializers.Binary;
    using Serializers.Json;
    using Serializers.XML;

    public static class ConfigureExtensions
    {
          public static string GetOrNull(this IDictionary<string,string> dictionary, string key)
          {
              if (!dictionary.ContainsKey(key))
                  return null;

              return dictionary[key];
          }
        public static Configure DefineTransport(this Configure config, string transport)
        {
            if (string.IsNullOrEmpty(transport))
                return config.MsmqTransport();

            var transportType = Type.GetType(transport);

            if (DefaultConnectionStrings.ContainsKey(transportType))
                return config.UseTransport(transportType, () => DefaultConnectionStrings[transportType]);
            else
                return config.UseTransport(transportType);
        }

        public static Configure DefineSerializer(this Configure config, string serializer)
        {
            if (string.IsNullOrEmpty(serializer))
                return config.XmlSerializer();

            var type = Type.GetType(serializer);

            if (type == typeof(XmlMessageSerializer))
                return config.XmlSerializer();


            if (type == typeof(JsonMessageSerializer))
                return config.JsonSerializer();


            if (type == typeof(BsonMessageSerializer))
                return config.BsonSerializer();

            if (type == typeof(MessageSerializer))
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
             
            if (type == typeof(WindsorObjectBuilder))
                return config.CastleWindsorBuilder();

            if (type == typeof(NinjectObjectBuilder))
                return config.NinjectBuilder();

            if (type == typeof(SpringObjectBuilder))
                return config.SpringFrameworkBuilder();

            if (type == typeof(StructureMapObjectBuilder))
                return config.StructureMapBuilder();

            if (type == typeof(UnityObjectBuilder))
                return config.StructureMapBuilder();

            
            throw new InvalidOperationException("Unknown builder:" + builder);
        }

        static Dictionary<Type, string> DefaultConnectionStrings = new Dictionary<Type, string>
            {
                { typeof(RabbitMQ), "host=localhost" },
                { typeof(SqlServer), @"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;" },
                { typeof(ActiveMQ),  @"activemq:tcp://localhost:61616" },
               
            };
    }
}