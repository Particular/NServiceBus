using System;
using Common.Logging;
using NServiceBus;
using Messages;
using NServiceBus.Unicast.Config;
using NServiceBus.Unicast.Transport.Msmq.Config;

namespace Client
{
    class Program
    {
        static void Main()
        {
            LogManager.GetLogger("hello").Debug("Started.");
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            new ConfigMsmqTransport(builder)
                .IsTransactional(false)
                .PurgeOnStartup(false)
                .UseXmlSerialization(false);

            new ConfigUnicastBus(builder)
                .ImpersonateSender(false);

            IBus bus = builder.Build<IBus>();

            bus.Start();

            Console.WriteLine("Press 'Enter' to send a message. To exit, press 'q' and then 'Enter'.");
            while (Console.ReadLine().ToLower() != "q")
            {
                RequestDataMessage m = new RequestDataMessage();
                m.DataId = Guid.NewGuid();

                Console.WriteLine("Requesting to get data by id: {0}", m.DataId);

                //notice that we're passing the message (m) as our state object
                bus.Send(m).Register(RequestDataComplete, m);
            }
        }

        private static void RequestDataComplete(IAsyncResult asyncResult)
        {
            CompletionResult result = asyncResult.AsyncState as CompletionResult;

            if (result == null)
                return;
            if (result.messages == null)
                return;
            if (result.messages.Length == 0)
                return;
            if (result.state == null)
                return;

            RequestDataMessage request = result.state as RequestDataMessage;
            if (request == null)
                return;

            DataResponseMessage response = result.messages[0] as DataResponseMessage;
            if (response == null)
                return;

            System.Diagnostics.Debug.Assert(request.DataId == response.DataId);
            Console.WriteLine("Response received with description: {0}",response.Description);
        }
    }
}
