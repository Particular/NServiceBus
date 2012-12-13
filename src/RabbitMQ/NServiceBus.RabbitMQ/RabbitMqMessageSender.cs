namespace NServiceBus.RabbitMQ
{
    using Unicast.Queuing;
    using Utils;
    using global::RabbitMQ.Client;

    public class RabbitMqMessageSender : ISendMessages
    {
        public IConnection Connection { get; set; }

        public void Send(TransportMessage message, Address address)
        {

            using (var channel = Connection.CreateModel())
            {
                //in 5.0 the message id will be set when the TM is created
                if(string.IsNullOrEmpty(message.Id))
                    message.Id = GuidCombGenerator.Generate().ToString();
  
                var properties = message.FillRabbitMqProperties(channel.CreateBasicProperties());

                channel.BasicPublish("", address.Queue, true, false, properties, message.Body);

            }
        }


    }
}