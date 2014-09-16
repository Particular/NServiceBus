namespace NServiceBus.Serializers.XML.Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    using MessageInterfaces.MessageMapper.Reflection;

    public class SerializerFactory
    {
         public static XmlMessageSerializer Create<T>(MessageMapper mapper = null)
         {
             var types = new List<Type> {typeof (T)};
             if (mapper == null)
             {
                 mapper = new MessageMapper();
             }

             mapper.Initialize(types);
             var serializer = new XmlMessageSerializer(mapper, new Conventions());

             serializer.Initialize(types);

             return serializer;
         }

         public static XmlMessageSerializer Create(params Type[] types)
         {
             var mapper = new MessageMapper();
             mapper.Initialize(types);
             var serializer = new XmlMessageSerializer(mapper, new Conventions());

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
            using (var stream = new MemoryStream())
            {
                SerializerFactory.Create<T>().Serialize(message, stream);
                stream.Position = 0;
              
                var msgArray = SerializerFactory.Create<T>().Deserialize(stream, new[]{message.GetType()});
                return (T)msgArray[0];

            }
        }

    }

    public class Serializer
    {
        string xmlResult;
        XmlDocument xmlDocument;

        Serializer(string result)
        {
            xmlResult = result;
        }

        public static Serializer Serialize<T>(Action<T> a) where T : class,new()
        {
            var msg = new T();
            a(msg);

            return ForMessage<T>(msg);
        }

        public static Serializer ForMessage<T>(object message,Action<XmlMessageSerializer> config = null)
        {
            using (var stream = new MemoryStream())
            {
                var serializer = SerializerFactory.Create<T>();
                    
                if(config != null)
                    config(serializer);


                serializer.Serialize(message, stream);
                stream.Position = 0;
                var result = new StreamReader(stream);

                return new Serializer(result.ReadToEnd());
            }
        }

        public Serializer AssertResultingXml(Func<XmlDocument, bool> check, string message)
        {
            if(xmlDocument == null)
            {
                xmlDocument = new XmlDocument();

                try
                {
                    xmlDocument.LoadXml(xmlResult);
                }
                catch (Exception ex)
                {
                    
                    throw new Exception("Failed to parse xml: " + xmlResult,ex);
                }
                

            }
            if (!check(xmlDocument))
              throw new Exception(string.Format("{0}, Offending XML: {1}",message, xmlResult));

            return this;
        }
    }
}
