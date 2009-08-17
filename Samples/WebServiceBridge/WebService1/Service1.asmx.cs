using System.Web.Services;
using System.ComponentModel;
using Messages;
using NServiceBus;

namespace WebService1
{
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    public class Service1 : NServiceBus.Webservice<Command, ErrorCodes>
    {
    }
}

