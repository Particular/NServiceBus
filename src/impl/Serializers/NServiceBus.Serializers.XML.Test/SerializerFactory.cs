namespace NServiceBus.Serializers.XML.Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using MessageInterfaces.MessageMapper.Reflection;
    using NUnit.Framework;
    using Serialization;

    public class SerializerFactory
    {
         public static IMessageSerializer Create<T>()
         {
             var types = new List<Type> {typeof (T)};
             var mapper = new MessageMapper();
             mapper.Initialize(types);
             var serializer = new XmlMessageSerializer(mapper);

             serializer.Initialize(types);

             return serializer;
         }

         public static IMessageSerializer Create(params Type[] types)
         {
             var mapper = new MessageMapper();
             mapper.Initialize(types);
             var serializer = new XmlMessageSerializer(mapper);

             serializer.Initialize(types);

             return serializer;
         }
    }

    public class ExecuteSerializer
    {
        public static T ForMessage<T>(Action<T> a) where T : class,new()
        {
            var msg = new T();
            a(msg);

            return ForMessage<T>(msg);
        }

        public static T ForMessage<T>(object message)
        {
            var serializer = SerializerFactory.Create<T>();

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(new[] { message }, stream);
                stream.Position = 0;

                var msgArray = serializer.Deserialize(stream);
                return (T)msgArray[0];

            }
        }

    }
}