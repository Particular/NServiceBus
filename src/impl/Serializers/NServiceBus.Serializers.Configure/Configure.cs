
using System;
using ObjectBuilder;

namespace NServiceBus.Serializers
{
    public class Configure : IWith
    {
        public static IWith XmlSerializer
        {
            get
            {
                Configure c = new Configure();
                c.serializerType = SerializerTypeEnum.XML;

                return c;
            }
        }

        public static IWith BinarySerializer
        {
            get
            {
                Configure c = new Configure();
                c.serializerType = SerializerTypeEnum.Binary;

                return c;
            }
        }

        public static IWith InterfaceToXMLSerializer
        {
            get
            {
                Configure c = new Configure();
                c.serializerType = SerializerTypeEnum.InterfaceToXML;

                return c;
            }
        }

        public void With(IBuilder builder)
        {
            switch(serializerType)
            {
                case SerializerTypeEnum.InterfaceToXML: builder.ConfigureComponent(typeof(InterfacesToXML.MessageSerializer), ComponentCallModelEnum.Singleton);
                    break;
                case SerializerTypeEnum.XML: builder.ConfigureComponent(typeof(XML.MessageSerializer), ComponentCallModelEnum.Singleton);
                    break;
                case SerializerTypeEnum.Binary: builder.ConfigureComponent(typeof(Binary.MessageSerializer), ComponentCallModelEnum.Singleton);
                    break;
            }
        }

        private Configure()
        {
            
        }


        private SerializerTypeEnum serializerType;
    }

    public enum SerializerTypeEnum
    {
        XML,
        Binary,
        InterfaceToXML
    }

    public interface IWith
    {
        void With(IBuilder builder);
    }
}
