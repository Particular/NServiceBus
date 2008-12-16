using NServiceBus.Grid.Messages;
using NServiceBus.Unicast.Transport;
using Common.Logging;


namespace NServiceBus.Grid.MessageHandlers
{
    public class GetNumberOfWorkerThreadsMessageHandler : IMessageHandler<GetNumberOfWorkerThreadsMessage>
    {
        public IBus Bus { get; set; }

        public void Handle(GetNumberOfWorkerThreadsMessage message)
        {
            int result = this.transport.NumberOfWorkerThreads;
            if (result == 1 && GridInterceptingMessageHandler.Disabled)
                result = 0;

            this.Bus.Reply(new GotNumberOfWorkerThreadsMessage { NumberOfWorkerThreads = result } );

            logger.Info(string.Format("{0} worker threads.", result));
        }

        private ITransport transport;
        public ITransport Transport
        {
            set
            {
                this.transport = value;
            }
        }

        private static readonly ILog logger = LogManager.GetLogger("NServicebus.Grid");
    }
}
