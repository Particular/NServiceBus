namespace NServiceBus.Transport.ActiveMQ.Receivers
{
    using Apache.NMS;
    using NServiceBus.Unicast.Transport.Transactional;

    public class ActiveMqMessageReceiver : INotifyMessageReceived
    {
        private readonly IConsumeEvents eventConsumer;
        private readonly IProcessMessages messageProcessor;
        private IMessageConsumer defaultConsumer;

        public ActiveMqMessageReceiver(
            IConsumeEvents eventConsumer,
            IProcessMessages messageProcessor)
        {
            this.eventConsumer = eventConsumer;
            this.messageProcessor = messageProcessor;
        }

        public void Start(Address address, TransactionSettings transactionSettings)
        {
            this.messageProcessor.Start(transactionSettings);

            this.defaultConsumer = this.messageProcessor.CreateMessageConsumer("queue://" + address.Queue);
            this.defaultConsumer.Listener += this.messageProcessor.ProcessMessage;

            if (address == Address.Local)
            {
                this.eventConsumer.Start();
            }
        }

        public void Stop()
        {
            this.messageProcessor.Stop();
            this.eventConsumer.Stop();
            this.defaultConsumer.Listener -= this.messageProcessor.ProcessMessage;
        }

        public void Dispose()
        {
            this.eventConsumer.Dispose();
            this.defaultConsumer.Close();
            this.defaultConsumer.Dispose();
            this.messageProcessor.Dispose();
        }
    }
}