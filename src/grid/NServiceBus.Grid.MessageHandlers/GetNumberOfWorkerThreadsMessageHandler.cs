using NServiceBus.Grid.Messages;
using NServiceBus.Unicast.Transport;
using Common.Logging;

namespace NServiceBus.Grid.MessageHandlers
{
    /// <summary>
    /// Handles the GetNumberOfWorkerThreadsMessage.
    /// </summary>
    public class GetNumberOfWorkerThreadsMessageHandler : IMessageHandler<GetNumberOfWorkerThreadsMessage>
    {
        /// <summary>
        /// The bus used for returning a response.
        /// </summary>
        public IBus Bus { get; set; }

        /// <summary>
        /// Reference to the local transport for getting the number of worker threads.
        /// </summary>
        public ITransport Transport { get; set; }

        /// <summary>
        /// Handles GetNumberOfWorkerThreadsMessage replying with GotNumberOfWorkerThreadsMessage.
        /// </summary>
        /// <param name="message"></param>
        public void Handle(GetNumberOfWorkerThreadsMessage message)
        {
            int result = this.Transport.NumberOfWorkerThreads;
            if (result == 1 && GridInterceptingMessageHandler.Disabled)
                result = 0;

            this.Bus.Reply(new GotNumberOfWorkerThreadsMessage { NumberOfWorkerThreads = result } );

            logger.Info(string.Format("{0} worker threads.", result));
        }

        private static readonly ILog logger = LogManager.GetLogger("NServicebus.Grid");
    }
}
