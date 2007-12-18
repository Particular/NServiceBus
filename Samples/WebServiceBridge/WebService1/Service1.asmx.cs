using System.Web.Services;
using System.ComponentModel;
using Messages;
using NServiceBus;
using System.Threading;
using System;
using NServiceBus.Async;

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

            ErrorCodes result = ErrorCodes.None;

            IAsyncResult sync = bClient.Send(request, delegate(IAsyncResult asyncResult)
                                      {
                                          CompletionResult completionResult = asyncResult.AsyncState as CompletionResult;
                                          if (completionResult != null)
                                          {
                                              result = (ErrorCodes) completionResult.errorCode;
                                          }
                                      },
                         Context.Response
                );

            sync.AsyncWaitHandle.WaitOne();

            return result;
        }
    }
}

