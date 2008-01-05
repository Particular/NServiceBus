using System.Web.Services;
using System.ComponentModel;
using Messages;
using NServiceBus;
using System;

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

            object result = ErrorCodes.None;

            IAsyncResult sync = bClient.Send(request).Register(
                delegate(IAsyncResult asyncResult)
                  {
                      CompletionResult completionResult = asyncResult.AsyncState as CompletionResult;
                      if (completionResult != null)
                      {
                          result = (ErrorCodes) completionResult.errorCode;
                      }
                  },
                  null
                  );

            sync.AsyncWaitHandle.WaitOne();

            return (ErrorCodes)result;
        }
    }
}

