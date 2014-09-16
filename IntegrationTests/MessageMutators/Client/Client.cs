using System;
using Messages;
using NServiceBus;

namespace Client
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Client
    {
        public void Customize(BusConfiguration configuration)
        {
            configuration.UsePersistence<InMemoryPersistence>();
        }
    }

    public class Runner : IWantToRunWhenBusStartsAndStops
    {
        public IBus Bus { get; set; }

        public void Start()
        {
            Console.WriteLine("Press 's' to send a valid message, press 'e' to send a failed message. To exit, 'q'\n");

            string cmd;

            while ((cmd = Console.ReadKey().Key.ToString().ToLower()) != "q")
            {
                switch (cmd)
                {
                    case "s":
                        Bus.Send<CreateProductCommand>(m =>
                        {
                            m.ProductId = "XJ128";
                            m.ProductName = "Milk";
                            m.ListPrice = 4;
                            m.SellEndDate = new DateTime(2012, 1, 3);
                            // 7MB. MSMQ should throw an exception, but it will not since the buffer will be compressed 
                            // before it reaches MSMQ.
                            m.Image = new byte[1024*1024*7];
                        });
                        break;
                    case "e":
                        try
                        {
                            Bus.Send<CreateProductCommand>(m =>
                            {
                                m.ProductId = "XJ128";
                                m.ProductName = "Milk Milk Milk Milk Milk";
                                m.ListPrice = 15;
                                m.SellEndDate = new DateTime(2011, 1, 3);
                                // 7MB. MSMQ should throw an exception, but it will not since the buffer will be compressed 
                                // before it reaches MSMQ.
                                m.Image = new byte[1024*1024*7];
                            });
                        }
                            //Just to allow the sample to keep running.
                        catch
                        {
                        }
                        break;
                }
            }
        }

        public void Stop()
        {
        }
    }
}