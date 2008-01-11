using NServiceBus;
using NServiceBus.Grid.Messages;
using NServiceBus.Unicast;
using NServiceBus.Unicast.Transport;


namespace NServiceBus.Grid.MessageHandlers
{
    /// <summary>
    /// Handles <see cref="ChangeNumberOfWorkerThreadsMessage"/>.
    /// </summary>
    public class ChangeNumberOfWorkerThreadsMessageHandler : 
        IMessageHandler<ChangeNumberOfWorkerThreadsMessage>
    {
        public void Handle(ChangeNumberOfWorkerThreadsMessage message)
        {
            int target = message.NumberOfWorkerThreads;
            if (target <= 0)
                target = 1;
            else
            {
                GridInterceptingMessageHandler.Disabled = false;
                this.unicastBus.ContinueSendingReadyMessages();
            }

            this.transport.ChangeNumberOfWorkerThreads(target);

            if (message.NumberOfWorkerThreads == 0)
            {
                this.unicastBus.StopSendingReadyMessages();
                GridInterceptingMessageHandler.Disabled = true;
            }
        }

        private IUnicastBus unicastBus;
        public IUnicastBus UnicastBus
        {
            set
            {
                this.unicastBus = value;
            }
        }

        private ITransport transport;

        /// <summary>
        /// This is kept separate from the bus because the distributor
        /// will be using this class on its control bus to change the
        /// number of worker threads on its data bus.
        /// 
        /// For regular cases, the transport should be the same as is
        /// configured for the bus.
        /// </summary>
        public ITransport Transport
        {
            set
            {
                this.transport = value;
            }
        }
    }
}
