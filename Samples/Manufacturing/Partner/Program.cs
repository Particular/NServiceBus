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
        private static IBus bus;
        private static void BusBootstrap()
        {
            bus = Configure.With()
                .Log4Net()
                .DefaultBuilder()
                .XmlSerializer()
                .MsmqTransport()
                    .IsTransactional(true)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .ImpersonateSender(false)
                    .LoadMessageHandlers()
                .CreateBus()
                .Start(() => Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>().Install());
        }

        static void Main()
        {
            try
            {
                BusBootstrap();
        
                Guid partnerId = Guid.NewGuid();
                Guid productId = Guid.NewGuid();
                
                List<IOrderLine> orderlines;

                Console.WriteLine("Enter the quantity you wish to order.\nSignal a complete PO with 'y'.\nTo exit, enter 'q'.");
                string line;
                string poId = Guid.NewGuid().ToString();
                while ((line = Console.ReadLine().ToLower()) != "q")
                {
                    if (line.ToLower().Contains("simulate"))
                        Simulate(bus, line.ToLower().Contains("step"));

                    bool done = (line == "y");
                    orderlines = new List<IOrderLine>(1);

                    if (!done)
                    {
                        float quantity; 
                        float.TryParse(line, out quantity);
                        orderlines.Add(ol => { ol.ProductId = productId; ol.Quantity = quantity; });
                    }

                    var orderMessage = new OrderMessage
                                                    {
                                                        Done = done,
                                                        OrderLines = orderlines,
                                                        PartnerId = partnerId,
                                                        ProvideBy = DateTime.UtcNow + TimeSpan.FromSeconds(10),
                                                        PurchaseOrderNumber = poId
                                                    };
                    bus.Send(orderMessage).Register<int>(i => Console.WriteLine("OK"));
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
            int numberOfOrders;
            int secondsToProvideBy;

            while(true)
            {
                var r = new Random();

                numberOfOrders = 5 + r.Next(0, 5);
                secondsToProvideBy = 10 + r.Next(0, 10);
                
                for (var i = 0; i < numberOfOrders; i++)
                {
                    var purchaseOrderNumber = Guid.NewGuid().ToString();

                    bus.Send<IOrderMessage>(m =>
                    {
                        m.PurchaseOrderNumber = purchaseOrderNumber;
                        m.PartnerId = partnerId;
                        m.Done = true;
                        m.ProvideBy = DateTime.UtcNow + TimeSpan.FromSeconds(secondsToProvideBy);
                        m.OrderLines = new List<IOrderLine> {
                            bus.CreateInstance<IOrderLine>(ol => { 
                                ol.ProductId = Guid.NewGuid(); 
                                ol.Quantity = (float) (Math.Sqrt(2)*r.Next(10));
                            })
                        };
                    });
                }

                Thread.Sleep(10);
                if (step)
                    Console.ReadLine();
            }
        }
    }
}
