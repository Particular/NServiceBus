namespace NServiceBus
{
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Json;
    using System.Text;

    static class HeaderSerializer
    {
        public static string Serialize(Dictionary<string, string> dictionary)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, true, true, "  "))
                {
                    serializer.WriteObject(writer, dictionary);
                    writer.Flush();
                }

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public static Dictionary<string, string> Deserialize(string value)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(value)))
            {
                return (Dictionary<string, string>)serializer.ReadObject(ms);
            }
        }

        static DataContractJsonSerializer serializer = new DataContractJsonSerializer(
            type: typeof(Dictionary<string, string>),
            settings: new DataContractJsonSerializerSettings
            {
                UseSimpleDictionaryFormat = true
            });
    }
}