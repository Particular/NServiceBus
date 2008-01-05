using System;
using NServiceBus.Grid.Messages;
using NServiceBus.Unicast.Transport;


namespace NServiceBus.Grid.MessageHandlers
{
    public class GetNumberOfWorkerThreadsMessageHandler : BaseMessageHandler<GetNumberOfWorkerThreadsMessage>
    {
        public override void Handle(GetNumberOfWorkerThreadsMessage message)
        {
            this.Bus.Reply(
                new GotNumberOfWorkerThreadsMessage(this.transport.NumberOfWorkerThreads)
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
