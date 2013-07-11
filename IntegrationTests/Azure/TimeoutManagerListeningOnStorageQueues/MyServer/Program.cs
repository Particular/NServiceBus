using NServiceBus.Timeout.Hosting.Azure;

namespace MyServer
{
    using System;
    using DeferedProcessing;
    using NServiceBus;
    using NServiceBus.Features;
    using Saga;

    public class Program
    {
        public static IBus Bus { get; set; }

        static void Main(string[] args)
        {
            BootstrapNServiceBus();

            Run();
        }

        public static void Run()
        {
            Console.WriteLine("Press 'S' to start the saga");
            Console.WriteLine("Press 'D' to defer a message 10 seconds");
            Console.WriteLine("To exit, press Ctrl + C");

            string cmd;

            while ((cmd = Console.ReadKey().Key.ToString().ToLower()) != "q")
            {
                switch (cmd)
                {
                    case "s":
                        StartSaga();
                        break;

                    case "d":
                        DeferMessage();
                        break;
                }
            }
        }

        static void DeferMessage()
        {
            Bus.Defer(TimeSpan.FromSeconds(10), new DeferredMessage());
            Console.WriteLine(string.Format("{0} - {1}", DateTime.Now.ToLongTimeString(), "Sent a message that is deferred for 10 seconds"));
        }

        static void StartSaga()
        {
            Bus.SendLocal(new StartSagaMessage
                              {
                                  OrderId = Guid.NewGuid()
                              });
            Console.WriteLine(string.Format("{0} - {1}", DateTime.Now.ToLongTimeString(), "Saga start message sent"));
        }


        private static void BootstrapNServiceBus()
        {
            Configure.Transactions.Enable();
            Configure.Features.Enable<Sagas>();
            Configure.Serialization.Json();

            Bus = Configure.With()
               .DefineEndpointName("MyServer")
               .DefaultBuilder()
               .AzureMessageQueue()
                .UseAzureTimeoutPersister()
               .AzureSagaPersister()
               .UnicastBus()
                    .LoadMessageHandlers()
               .CreateBus()
               .Start();
        }
    }
}