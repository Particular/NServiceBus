using System;
using System.Collections.Generic;
using System.Text;
using Common.Logging;
using NServiceBus;
using NServiceBus.Unicast.Config;
using NServiceBus.Unicast.Transport.Msmq.Config;
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
                NServiceBus.Serializers.Configure.BinarySerializer.With(builder);
                //NServiceBus.Serializers.Configure.XmlSerializer.With(builder);

                new ConfigMsmqTransport(builder)
                    .IsTransactional(true)
                    .PurgeOnStartup(false);

                new ConfigUnicastBus(builder)
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
                    bool done = (line == "y");
                    orderlines = new List<OrderLine>(1);

                    if (!done)
                    {
                        float.TryParse(line, out quantity);
                        orderlines.Add(new OrderLine(productId, quantity));
                    }

                    OrderMessage m = new OrderMessage(
                        poId, 
                        partnerId,
                        done,
                        DateTime.Now + TimeSpan.FromSeconds(10),
                        orderlines
                        );
                    

                    bus.Send(m);

                    Console.WriteLine("Send PO Number {0}.", m.PurchaseOrderNumber);

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
    }
}
