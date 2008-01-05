using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Xml;

namespace NServiceBus.Unicast.Transport.WCF
{
    public class XmlSerializerWrapper : XmlObjectSerializer
    {
        private XmlSerializer serializer;

        public XmlSerializerWrapper(Type type, IEnumerable<Type> extraTypes)
        {
            List<Type> types = new List<Type>(extraTypes);
            foreach (Type t in extraTypes)
                if (this.CantSerializeType(t))
                    types.Remove(t);

            if (!types.Contains(typeof(List<object>)))
                types.Add(typeof(List<object>));

            this.serializer = new XmlSerializer(type, types.ToArray());
        }

        private bool CantSerializeType(Type t)
        {
            if (t.IsInterface || t.IsAbstract)
                return true;

            if (t.IsArray)
                return CantSerializeType(t.GetElementType());

            return false;
        }

        public override bool IsStartObject(XmlDictionaryReader reader)
        {
            throw new NotImplementedException();
        }

        public override object ReadObject(XmlDictionaryReader reader, bool verifyObjectName)
        {
            throw new NotImplementedException();
        }
        public override void WriteEndObject(XmlDictionaryWriter writer)
        {
            throw new NotImplementedException();
        }

        public override void WriteObjectContent(XmlDictionaryWriter writer, object graph)
        {
            throw new NotImplementedException();
        }

        public override void WriteStartObject(XmlDictionaryWriter writer, object graph)
        {
            throw new NotImplementedException();
        }

        public override void WriteObject(XmlDictionaryWriter writer, object graph)
        {
            this.serializer.Serialize(writer, graph);
        }

        public override object ReadObject(XmlDictionaryReader reader)
        {
            object result = this.serializer.Deserialize(reader);

            TransportMessage m = result as TransportMessage;
            if (m != null)
                m.CopyMessagesToBody();

            return result;
        }
    } 
}
