using System.Threading;
using MyMessages;
using NServiceBus;
using NServiceBus.Logging;

namespace MyServer
{
    public class RequestDataMessageHandler : IHandleMessages<RequestDataMessage>
    {
        public IBus Bus { get; set; }

        public void Handle(RequestDataMessage message)
        {
            //try to uncomment the line below to see the error handling in action
            // 1. nservicebus will retry the configured number of times configured in app.config
            // 2. the UoW will rollback
            // 3. When the max retries is reached the message will be given to the faultmanager (in memory in this case)
            //throw new Exception("Database connection lost");

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

            Bus.SetMessageHeader(response, "Test", Bus.GetMessageHeader(message, "Test"));
            response.SetHeader("1", "1");
            response.SetHeader("2", "2");

            Bus.Reply(response); //Try experimenting with sending multiple responses
        }

        public static ILog Logger = LogManager.GetLogger("MyServer");
    }
}
