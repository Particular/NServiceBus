namespace NServiceBus
{
    using System.Xml.Linq;

    interface IInstanceMappingFileAccess
    {
        XDocument Load(string path);
    }
}