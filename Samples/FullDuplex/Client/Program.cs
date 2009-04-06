using System;
using Common.Logging;
using NServiceBus;
using Messages;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.ObjectBuilder;
using System.Collections.Generic;

namespace Client
{
    class Program
    {
        private static IBus bus = null;

        static void Main()
        {
            LogManager.GetLogger("hello").Debug("Started.");

            bus = NServiceBus.Configure.With()
                .SpringBuilder()
                .XmlSerializer("http://www.UdiDahan.com")
                .MsmqTransport()
                    .IsTransactional(false)
                    .PurgeOnStartup(true)
                .UnicastBus()
                    .ImpersonateSender(false)
                .CreateBus()
                .Start();


            Console.WriteLine("Press 'Enter' to send a message. To exit, press 'q' and then 'Enter'.");
            while (Console.ReadLine().ToLower() != "q")
            {
                RequestDataMessage m = new RequestDataMessage();

                m.DataId = Guid.NewGuid();
                m.String = "<node>it's my \"node\" & i like it<node>";

                Console.WriteLine("Requesting to get data by id: {0}", m.DataId.ToString("N"));

                bus.OutgoingHeaders["Test"] = m.DataId.ToString("N");

                //notice that we're passing the message as our state object
                bus.Send(m).Register(RequestDataComplete, m);
            }
        }

        private static void RequestDataComplete(IAsyncResult asyncResult)
        {
            Console.Out.WriteLine("Response with header 'Test' = {0}, 1 = {1}, 2 = {2}.", bus.IncomingHeaders["Test"], bus.IncomingHeaders["1"], bus.IncomingHeaders["2"]);

            CompletionResult result = asyncResult.AsyncState as CompletionResult;

            if (result == null)
                return;
            if (result.Messages == null)
                return;
            if (result.Messages.Length == 0)
                return;
            if (result.State == null)
                return;

            DataResponseMessage response = result.Messages[0] as DataResponseMessage;
            if (response == null)
                return;

            Console.WriteLine("Response received with description: {0}",response.String);
        }
    }
}
