namespace NServiceBus.Transports.ActiveMQ.Receivers
{
    using System;
    using Apache.NMS;
    using NServiceBus.Logging;
    using Unicast.Transport;

    public class ActiveMqMessageReceiver : INotifyMessageReceived
    {
        private readonly IConsumeEvents eventConsumer;
        private readonly IProcessMessages messageProcessor;
        private IMessageConsumer defaultConsumer;
        private bool disposed;

        public ActiveMqMessageReceiver(
            IConsumeEvents eventConsumer,
            IProcessMessages messageProcessor)
        {
            this.eventConsumer = eventConsumer;
            this.messageProcessor = messageProcessor;
        }

        public void Dispose()
        {
            if (this.disposed) return;

            try
            {
                if (eventConsumer != null)
                {
                    eventConsumer.Dispose();
                }
                if (defaultConsumer != null)
                {
                    defaultConsumer.Dispose();
                }
                if (messageProcessor != null)
                {
                    messageProcessor.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to dispose the receiver",ex);
            }

            disposed = true;
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

        static ILog Logger = LogManager.GetLogger(typeof(ActiveMqMessageReceiver));
    }
}