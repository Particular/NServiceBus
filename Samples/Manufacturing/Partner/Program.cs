using System;
using System.Collections.Generic;
using System.Threading;
using Common.Logging;
using NServiceBus;
using NServiceBus.Config;
using OrderService.Messages;

namespace Partner
{
    class Program
    {
        static void Main()
        {
            LogManager.GetLogger("hello").Debug("Partner Started.");
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            try
            {
                NServiceBus.Config.Configure.With(builder)
                    .InterfaceToXMLSerializer()
                    .MsmqTransport()
                        .IsTransactional(true)
                        .PurgeOnStartup(false)
                    .UnicastBus()
                        .ImpersonateSender(false)
                        .SetMessageHandlersFromAssembliesInOrder(
                            typeof(OrderStatusChangedMessageHandler).Assembly
                        );

                IBus bus = builder.Build<IBus>();
                bus.Start();

                Guid partnerId = Guid.NewGuid();
                Guid productId = Guid.NewGuid();
                float quantity = 10.0F;
                List<OrderLine> orderlines;

                Console.WriteLine("Enter the quantity you wish to order.\nSignal a complete PO with 'y'.\nTo exit, enter 'q'.");
                string line;
                string poId = Guid.NewGuid().ToString();
                while ((line = Console.ReadLine().ToLower()) != "q")
                {
                    if (line == "simulate")
                        Simulate(bus);

                    bool done = (line == "y");
                    orderlines = new List<OrderLine>(1);

                    if (!done)
                    {
                        float.TryParse(line, out quantity);
                        orderlines.Add<OrderLine>(ol => { ol.ProductId = productId; ol.Quantity = quantity; });
                    }

                    bus.Send<OrderMessage>(m =>
                    {
                        m.PurchaseOrderNumber = poId;
                        m.PartnerId = partnerId;
                        m.Done = done;
                        m.ProvideBy = DateTime.Now + TimeSpan.FromSeconds(10);
                        m.OrderLines = orderlines;
                    });

                    Console.WriteLine("Send PO Number {0}.", poId);

                    if (done)
                        poId = Guid.NewGuid().ToString();
                }
            }
            catch (Exception e)
            {
                LogManager.GetLogger("hello").Fatal("Exiting", e);
                Console.Read();
            }
        }

        private static void Simulate(IBus bus)
        {
            Guid partnerId = Guid.NewGuid();

            int numberOfLines;
            int secondsToProvideBy;

            while(true)
            {
                Random r = new Random();

                numberOfLines = 5 + r.Next(0, 5);
                secondsToProvideBy = 5 + r.Next(0, 5);
                string purchaseOrderNumber = Guid.NewGuid().ToString();

                for (int i = 0; i < numberOfLines; i++)
                {
                    bus.Send<OrderMessage>(m =>
                    {
                        m.PurchaseOrderNumber = purchaseOrderNumber;
                        m.PartnerId = partnerId;
                        m.Done = (i == numberOfLines - 1);
                        m.ProvideBy = DateTime.Now + TimeSpan.FromSeconds(secondsToProvideBy);
                        m.OrderLines = new List<OrderLine> {
                            bus.CreateInstance<OrderLine>(ol => { 
                                ol.ProductId = Guid.NewGuid(); 
                                ol.Quantity = (float) (Math.Sqrt(2)*r.Next(10));
                            })
                        };
                    });
                }

                Thread.Sleep(1000);
            }
        }
    }
}
