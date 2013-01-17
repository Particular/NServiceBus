namespace NServiceBus.Transport.ActiveMQ
{
    public class MessageProducer : IMessageProducer
    {
        private readonly IActiveMqMessageMapper activeMqMessageMapper;
        private readonly ISessionFactory sessionFactory;
        private readonly IDestinationEvaluator destinationEvaluator;

        public MessageProducer(
            ISessionFactory sessionFactory, 
            IActiveMqMessageMapper activeMqMessageMapper,
            IDestinationEvaluator destinationEvaluator)
        {
            this.sessionFactory = sessionFactory;
            this.activeMqMessageMapper = activeMqMessageMapper;
            this.destinationEvaluator = destinationEvaluator;
        }

        public void SendMessage(TransportMessage message, string destination, string destinationPrefix)
        {
            var session = this.sessionFactory.GetSession();
            try
            {
                var jmsMessage = this.activeMqMessageMapper.CreateJmsMessage(message, session);

                using (var producer = session.CreateProducer())
                {
                    producer.Send(this.destinationEvaluator.GetDestination(session, destination, destinationPrefix), jmsMessage);
                }
            }
            finally
            {
                this.sessionFactory.Release(session);
            }
        }        
    }
}