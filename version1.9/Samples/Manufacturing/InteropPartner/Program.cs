using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Messaging;
using System.Xml.Serialization;

namespace InteropPartner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Using straight xml serialization and msmq to test interop.");
            Console.WriteLine("To exit, enter 'q'. Press 'Enter' to send a message.");
            
            string queueName = string.Format("FormatName:DIRECT=OS:{0}\\private$\\messagebus", Environment.MachineName);

            MessageQueue q = new MessageQueue(queueName);

            XmlSerializer serializer = new XmlSerializer(typeof(OrderMessage), new Type[] { typeof(OrderLine) });

            string line = null;
            while ((line = Console.ReadLine().ToLower()) != "q")
            {
                OrderMessage m1 = new OrderMessage();
                m1.PurchaseOrderNumber = Guid.NewGuid().ToString();
                m1.ProvideBy = DateTime.Now;
                m1.PartnerId = Guid.NewGuid();
                m1.OrderLines = new OrderLine[] { new OrderLine { ProductId = Guid.NewGuid(), Quantity = 10F } };
                m1.Done = true;

                Message toSend = new Message();
                serializer.Serialize(toSend.BodyStream, m1);

                q.Send(toSend, MessageQueueTransactionType.Single);
                Console.WriteLine("Sent order {0}", m1.PurchaseOrderNumber);
            }
        }
    }
}
