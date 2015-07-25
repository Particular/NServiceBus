namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System.Messaging;
    using System.Text;

    public class Cant_convert : NServiceBusAcceptanceTest
    {
        protected void SendCorruptedMessage(string queueName)
        {
            var path = string.Format(@".\private$\{0}", queueName);
            using (var queue = new MessageQueue(path))
            {
                var message = new Message("")
                {
                    Extension = Encoding.UTF8.GetBytes("Can't deserialize this")
                };
                queue.Send(message, MessageQueueTransactionType.Single);
            }
        }
    }
}