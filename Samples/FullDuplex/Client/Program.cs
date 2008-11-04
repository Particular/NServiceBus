using System;
using Common.Logging;
using NServiceBus;
using Messages;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.Unicast.Config;
using NServiceBus.Unicast.Transport.Msmq.Config;
using ObjectBuilder;
using NServiceBus.Serialization;
using NServiceBus.MessageInterfaces;

namespace Client
{
    class Program
    {
        private static IBus bus = null;

        static void Main()
        {
            LogManager.GetLogger("hello").Debug("Started.");
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            ConfigureSelfWith(builder);

            bus = builder.Build<IBus>();

            bus.Start();

            bus.OutgoingHeaders["Test"] = "client";

            Console.WriteLine("Press 'Enter' to send a message. To exit, press 'q' and then 'Enter'.");
            while (Console.ReadLine().ToLower() != "q")
            {
                RequestDataMessage m = new RequestDataMessage();
                //IRequestDataMessage r = builder.Build<IMessageCreator>().CreateInstance<IRequestDataMessage>();

                m.DataId = Guid.NewGuid();
                //r.DataId = m.DataId;

                Console.WriteLine("Requesting to get data by id: {0}", m.DataId);

                //notice that we're passing the message as our state object
                bus.Send(m).Register(RequestDataComplete, m);
                //bus.Send(r).Register(RequestDataComplete, r);
            }
        }

        private static void RequestDataComplete(IAsyncResult asyncResult)
        {
            Console.Out.WriteLine("Header 'Test' = {0}, 1 = {1}, 2 = {2}.", bus.IncomingHeaders["Test"], bus.IncomingHeaders["1"], bus.IncomingHeaders["2"]);

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

            Console.WriteLine("Response received with description: {0}",response.Description);
        }

        private static void ConfigureSelfWith(IBuilder builder)
        {
            //NServiceBus.Serializers.Configure.InterfaceToXMLSerializer.WithNameSpace("http://www.UdiDahan.com").With(builder);
            NServiceBus.Serializers.Configure.XmlSerializer.WithNameSpace("http://www.UdiDahan.com").With(builder);

            new ConfigMsmqTransport(builder)
                .IsTransactional(false)
                .PurgeOnStartup(false);

            new ConfigUnicastBus(builder)
                .ImpersonateSender(false);

            builder.ConfigureComponent<MessageMapper>(ComponentCallModelEnum.Singleton);
        }
    }
}
