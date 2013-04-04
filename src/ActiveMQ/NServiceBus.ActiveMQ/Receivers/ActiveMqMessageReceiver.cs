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

        ~ActiveMqMessageReceiver()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
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

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed) return;

            try
            {
                if (disposing)
                {
                    this.eventConsumer.Dispose();
                    this.defaultConsumer.Dispose();
                    this.messageProcessor.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to dispose the receiver",ex);
            }

            disposed = true;
        }

        static ILog Logger = LogManager.GetLogger(typeof(ActiveMqMessageReceiver));
    }
}