using System;
using System.Collections.Generic;
using System.Threading;
using log4net;
using NServiceBus;
using OrderService.Messages;

namespace Partner
{
    class Program
    {
        static void Main()
        {
            try
            {
                var bus = NServiceBus.Configure.With()
                    .DefaultBuilder()
                    .XmlSerializer()
                    .MsmqTransport()
                        .IsTransactional(true)
                        .PurgeOnStartup(false)
                    .UnicastBus()
                        .ImpersonateSender(false)
                        .LoadMessageHandlers()
                    .CreateBus()
                    .Start();

                Guid partnerId = Guid.NewGuid();
                Guid productId = Guid.NewGuid();
                float quantity = 10.0F;
                List<OrderLine> orderlines;

                Console.WriteLine("Enter the quantity you wish to order.\nSignal a complete PO with 'y'.\nTo exit, enter 'q'.");
                string line;
                string poId = Guid.NewGuid().ToString();
                while ((line = Console.ReadLine().ToLower()) != "q")
                {
                    if (line.ToLower().Contains("simulate"))
                        Simulate(bus, line.ToLower().Contains("step"));

                    bool done = (line == "y");
                    orderlines = new List<OrderLine>(1);

                    if (!done)
                    {
                        float.TryParse(line, out quantity);
                        orderlines.Add(ol => { ol.ProductId = productId; ol.Quantity = quantity; });
                    }

                    bus.Send<OrderMessage>(m =>
                    {
                        m.PurchaseOrderNumber = poId;
                        m.PartnerId = partnerId;
                        m.Done = done;
                        m.ProvideBy = DateTime.Now + TimeSpan.FromSeconds(10);
                        m.OrderLines = orderlines;
                    }).Register(i => Console.WriteLine("OK"));

                    Console.WriteLine("Send PO Number {0}.", poId);

                    if (done)
                        poId = Guid.NewGuid().ToString();
                }
            }
            catch (Exception e)
            {
                LogManager.GetLogger("Partner").Fatal("Exiting", e);
                Console.Read();
            }
        }

        private static void Simulate(IBus bus, bool step)
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
                if (step)
                    Console.ReadLine();
            }
        }
    }
}
