namespace NServiceBus
{
    using System.Xml.Linq;

    interface IRoutingFileAccess
    {
        XDocument Load(string path);
    }
}