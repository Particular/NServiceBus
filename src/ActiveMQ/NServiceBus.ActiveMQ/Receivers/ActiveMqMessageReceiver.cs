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
            if (disposed) return;

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
            messageProcessor.Start(transactionSettings);

            defaultConsumer = messageProcessor.CreateMessageConsumer("queue://" + address.Queue);
            defaultConsumer.Listener += messageProcessor.ProcessMessage;

            if (address == Address.Local)
            {
                eventConsumer.Start();
            }
        }

        public void Stop()
        {
            messageProcessor.Stop();
            eventConsumer.Stop();
            defaultConsumer.Listener -= messageProcessor.ProcessMessage;
        }

        static ILog Logger = LogManager.GetLogger(typeof(ActiveMqMessageReceiver));
    }
}