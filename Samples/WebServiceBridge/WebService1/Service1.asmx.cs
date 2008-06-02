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
            object result = ErrorCodes.None;

            IAsyncResult sync = Global.Bus.Send(request).Register(
                delegate(IAsyncResult asyncResult)
                  {
                      CompletionResult completionResult = asyncResult.AsyncState as CompletionResult;
                      if (completionResult != null)
                      {
                          result = (ErrorCodes) completionResult.ErrorCode;
                      }
                  },
                  null
                  );

            sync.AsyncWaitHandle.WaitOne();

            return (ErrorCodes)result;
        }
    }
}

