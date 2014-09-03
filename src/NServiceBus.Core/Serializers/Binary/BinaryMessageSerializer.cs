namespace NServiceBus.Serializers.Binary
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Xml.Linq;
    using Serialization;

    class BinaryMessageSerializer : IMessageSerializer
    {
        public BinaryMessageSerializer()
        {
            var surrogateSelector = new SurrogateSelector();
            surrogateSelector.AddSurrogate(typeof(XDocument), new StreamingContext(StreamingContextStates.All), new XContainerSurrogate());
            surrogateSelector.AddSurrogate(typeof(XElement), new StreamingContext(StreamingContextStates.All), new XElementSurrogate());

            binaryFormatter.SurrogateSelector = surrogateSelector;
        }
        
        public void Serialize(object message, Stream stream)
        {
            binaryFormatter.Serialize(stream, new List<object> { message });
        }

        public object[] Deserialize(Stream stream, IList<Type> messageTypes = null)
        {
            if (stream == null)
                return null;

            var body = binaryFormatter.Deserialize(stream) as List<object>;

            if (body == null)
                return null;

            var result = new object[body.Count];

            var i = 0;
            foreach (var m in body)
                result[i++] = m;

            return result;
        }

        public string ContentType { get{ return ContentTypes.Binary;}}

        readonly BinaryFormatter binaryFormatter = new BinaryFormatter();
    }
}
