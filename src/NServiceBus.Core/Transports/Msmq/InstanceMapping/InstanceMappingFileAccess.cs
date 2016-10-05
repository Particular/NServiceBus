namespace NServiceBus
{
    using System.IO;
    using System.Xml;
    using System.Xml.Linq;

    class InstanceMappingFileAccess : IInstanceMappingFileAccess
    {
        public XDocument Load(string path)
        {
            XDocument doc;
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = XmlReader.Create(file))
                {
                    doc = XDocument.Load(reader);
                }
            }
            return doc;
        }
    }
}