using System;
using System.Threading;
using log4net;
using MyMessages;
using NServiceBus;

namespace MyServer
{
    public class RequestDataMessageHandler : IHandleMessages<RequestDataMessage>,
        IAmResponsibleForMessages<RequestDataMessage>
    {
        public IBus Bus { get; set; }

        public void Handle(RequestDataMessage message)
        {
            Logger.Info("==========================================================================");
            Logger.InfoFormat("Received request {0}.", message.DataId);
            Logger.InfoFormat("String received: {0}.", message.String);
            Logger.InfoFormat("Header 'Test' = {0}.", message.GetHeader("Test"));
            Logger.InfoFormat(Thread.CurrentPrincipal != null ? Thread.CurrentPrincipal.Identity.Name : string.Empty);

            var response = Bus.CreateInstance<DataResponseMessage>(m => 
            { 
                m.DataId = message.DataId;
                m.String = message.String;
            });

            response.CopyHeaderFromRequest("Test");
            response.SetHeader("1", "1");
            response.SetHeader("2", "2");

            Bus.Reply(response); //Try experimenting with sending multiple responses
        }

        public static ILog Logger = LogManager.GetLogger("MyServer");
    }
}
