namespace NServiceBus.Serializers.XML.Test
{
    using System;
    using System.Collections.Generic;
    using MessageInterfaces.MessageMapper.Reflection;

    public class SerializerFactory
    {
         public static XmlMessageSerializer Create<T>()
         {
             var types = new List<Type> {typeof (T)};
             var mapper = new MessageMapper();
             mapper.Initialize(types);
             var serializer = new XmlMessageSerializer(mapper);

             serializer.Initialize(types);

             return serializer;
         }

         public static XmlMessageSerializer Create(params Type[] types)
         {
             var mapper = new MessageMapper();
             mapper.Initialize(types);
             var serializer = new XmlMessageSerializer(mapper);

             serializer.Initialize(types);

             return serializer;
         }
    }
}
