namespace NServiceBus
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;

    //todo: switch to the json serializer
    static class HeaderSerializer
    {
        static HeaderSerializer()
        {
            emptyNamespace = new XmlSerializerNamespaces();
            emptyNamespace.Add("", "");
            serializer = new System.Xml.Serialization.XmlSerializer(typeof(List<DevelopmentTransportHeader>), new XmlRootAttribute("Headers"));
        }

        public static string Serialize(Dictionary<string, string> dictionary)
        {
            var builder = new StringBuilder();
            using (var writer = new StringWriter(builder))
            {
                var headers = dictionary.Select(x => new DevelopmentTransportHeader
                {
                    Key = x.Key,
                    Value = x.Value
                }).ToList();
                serializer.Serialize(writer, headers, emptyNamespace);
            }
            return builder.ToString();
        }

        public static Dictionary<string, string> Deserialize(string value)
        {
            using (var reader = new StringReader(value))
            {
                var list = (List<DevelopmentTransportHeader>) serializer.Deserialize(reader);
                return list.ToDictionary(header => header.Key, header => header.Value);
            }
        }

        static System.Xml.Serialization.XmlSerializer serializer;
        static XmlSerializerNamespaces emptyNamespace;
    }

    /// <summary>
    /// DTO for the serializing headers.
    /// </summary>
    public class DevelopmentTransportHeader
    {
        /// <summary>
        /// The header key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The header value.
        /// </summary>
        public string Value { get; set; }
    }
}