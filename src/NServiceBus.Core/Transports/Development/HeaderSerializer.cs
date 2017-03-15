namespace NServiceBus
{
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Json;
    using System.Text;

    static class HeaderSerializer
    {
        static HeaderSerializer()
        {
            serializer = new DataContractJsonSerializer(typeof(Dictionary<string, string>));
        }

        public static string Serialize(Dictionary<string, string> dictionary)
        {
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, dictionary);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public static Dictionary<string, string> Deserialize(string value)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(value)))
            {
                return (Dictionary<string, string>) serializer.ReadObject(ms);
            }
        }

        static DataContractJsonSerializer serializer;
    }
}