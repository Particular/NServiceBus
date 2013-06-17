using System;
using NServiceBus;
using Receiver.Messages;

namespace Sender
{
    class Program
    {
        private static IBus bus;

        static void Main(string[] args)
        {
            BootstrapNServiceBus();

            string key = "";

            while (key != "q")
            {
                Console.WriteLine("Press 'q' to quit, Use 'e' to send a message that will exceed the limit and throw, or any other key to send a large message.");
                
                key = Console.ReadLine();

                if(key == "q") continue;
                if (key == "e") SendMessageThatIsLargerThanQueueStorageCanHandle();
                else SendMessageThroughDataBus();

            }
        }

        private static void SendMessageThroughDataBus()
        {
            bus.Send<MessageWithLargePayload>(m =>
            {
                m.SomeProperty = "This message contains a large blob that will be sent on the data bus";
                m.LargeBlob = new DataBusProperty<byte[]>(new byte[1024 * 1024 * 5]);//5MB
            });
        }

        private static void SendMessageThatIsLargerThanQueueStorageCanHandle()
        {
            try
            {
                bus.Send<AnotherMessageWithLargePayload>(m =>
                {
                    m.LargeBlob = new byte[1024 * 1024 * 5];//5MB
                });

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }
        private static void BootstrapNServiceBus()
        {
            Configure.Transactions.Enable();
            Configure.Serialization.Binary();

            bus = Configure.With()
               .DefaultBuilder()
               .AzureMessageQueue()
               .AzureDataBus()
               .UnicastBus()
                    .LoadMessageHandlers()
               .CreateBus()
               .Start();
        }
    }
}
