using System.Web.Services;
using System.ComponentModel;
using Messages;
using NServiceBus;
using System.Threading;

namespace WebService1
{
    /// <summary>
    /// Summary description for Service1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    public class Service1 : WebService
    {

        [WebMethod]
        public ErrorCodes Process(Command request)
        {
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            IBus bClient = builder.Build<IBus>();

            ManualResetEvent mre = new ManualResetEvent(false);
            ErrorCodes result = ErrorCodes.None;

            bClient.Send(request, delegate(int error, object state)
                                      {
                                          result = (ErrorCodes) error;
                                          mre.Set();
                                      },
                         Context.Response
                );

            mre.WaitOne();

            return result;
        }
    }
}

