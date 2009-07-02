using System;
using System.Messaging;
using System.Xml.Serialization;

namespace InteropPartner
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Using straight xml serialization and msmq to test interop.");
            Console.WriteLine("To exit, enter 'q'. Press 'Enter' to send a message.");
            
            string queueName = string.Format("FormatName:DIRECT=OS:{0}\\private$\\messagebus", Environment.MachineName);

            var q = new MessageQueue(queueName);

            var serializer = new XmlSerializer(typeof(OrderMessage), new[] { typeof(OrderLine) });

            while ((Console.ReadLine().ToLower()) != "q")
            {
                var m1 = new OrderMessage
                             {
                                 PurchaseOrderNumber = Guid.NewGuid().ToString(),
                                 ProvideBy = DateTime.Now,
                                 PartnerId = Guid.NewGuid(),
                                 OrderLines = new[] {new OrderLine {ProductId = Guid.NewGuid(), Quantity = 10F}},
                                 Done = true
                             };

                var toSend = new Message();
                serializer.Serialize(toSend.BodyStream, m1);
                toSend.ResponseQueue = new MessageQueue(string.Format("FormatName:DIRECT=OS:{0}\\private$\\client", Environment.MachineName));

                q.Send(toSend, MessageQueueTransactionType.Single);
                Console.WriteLine("Sent order {0}", m1.PurchaseOrderNumber);
            }
        }
    }
}
