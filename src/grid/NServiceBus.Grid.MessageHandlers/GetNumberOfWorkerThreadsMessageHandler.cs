using NServiceBus.Grid.Messages;
using NServiceBus.Unicast.Transport;


namespace NServiceBus.Grid.MessageHandlers
{
    public class GetNumberOfWorkerThreadsMessageHandler : BaseMessageHandler<GetNumberOfWorkerThreadsMessage>
    {
        public override void Handle(GetNumberOfWorkerThreadsMessage message)
        {
            int result = this.transport.NumberOfWorkerThreads;
            if (result == 1 && GridInterceptingMessageHandler.Disabled)
                result = 0;

            this.Bus.Reply(
                new GotNumberOfWorkerThreadsMessage(result)
            );
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
