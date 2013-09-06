namespace NServiceBus.Transports.ActiveMQ.Receivers
{
    using System;
    using Apache.NMS;
    using Logging;
    using Unicast.Transport;

    public class ActiveMqMessageReceiver : INotifyMessageReceived
    {
        IConsumeEvents eventConsumer;
        IProcessMessages messageProcessor;
        IMessageConsumer defaultConsumer;
        
        public ActiveMqMessageReceiver(IConsumeEvents eventConsumer,IProcessMessages messageProcessor)
        {
            this.eventConsumer = eventConsumer;
            this.messageProcessor = messageProcessor;
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        public void DisposeManaged()
        {
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