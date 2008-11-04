
using System;
using ObjectBuilder;

namespace NServiceBus.Serializers
{
    public class Configure : IWith, IWithNameSpace
    {
        public static IWithNameSpace XmlSerializer
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

        public static IWithNameSpace InterfaceToXMLSerializer
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
                case SerializerTypeEnum.InterfaceToXML:
                    builder.ConfigureComponent<InterfacesToXML.MessageSerializer>(ComponentCallModelEnum.Singleton)
                        .Namespace = this.ns;
                    break;
                case SerializerTypeEnum.XML:
                    builder.ConfigureComponent<XML.MessageSerializer>(ComponentCallModelEnum.Singleton)
                        .Namespace = this.ns;
                    break;
                case SerializerTypeEnum.Binary: builder.ConfigureComponent(typeof(Binary.MessageSerializer), ComponentCallModelEnum.Singleton);
                    break;
            }
        }

        private Configure()
        {
            
        }


        private SerializerTypeEnum serializerType;

        public IWith WithNameSpace(string nameSpace)
        {
            this.ns = nameSpace;
            return this;
        }

        private string ns = "http://tempuri.net";
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

    public interface IWithNameSpace : IWith
    {
        IWith WithNameSpace(string nameSpace); 
    }
}
