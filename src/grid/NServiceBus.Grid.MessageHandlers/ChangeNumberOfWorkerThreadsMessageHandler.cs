using NServiceBus;
using NServiceBus.Grid.Messages;
using NServiceBus.Unicast.Transport;


namespace NServiceBus.Grid.MessageHandlers
{
    /// <summary>
    /// Handles <see cref="ChangeNumberOfWorkerThreadsMessage"/> and <see cref="IMessage"/>.
    /// Should be configured to run before regular logic to enable
    /// stopping and restarting the bus remotely.
    /// </summary>
    public class ChangeNumberOfWorkerThreadsMessageHandler : 
        BaseMessageHandler<ChangeNumberOfWorkerThreadsMessage>
    {
        public override void Handle(ChangeNumberOfWorkerThreadsMessage message)
        {
            int target = message.NumberOfWorkerThreads;
            if (target <= 0)
                target = 1;
            else
            {
                GridInterceptingMessageHandler.Disabled = false;
                this.transport.ContinueSendingReadyMessages();
            }

            this.transport.ChangeNumberOfWorkerThreads(target);

            if (message.NumberOfWorkerThreads == 0)
            {
                this.transport.StopSendingReadyMessages();
                GridInterceptingMessageHandler.Disabled = true;
            }
        }

        private ITransport transport;
        public ITransport Transport
        {
            set
            {
                this.transport = value;
            }
        }
    }
}
