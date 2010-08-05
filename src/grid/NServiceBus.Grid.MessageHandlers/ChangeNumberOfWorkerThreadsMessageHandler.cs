using Common.Logging;
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
        /// <summary>
        /// If the target number of worker threads in the message is zero,
        /// brings them down to one, and marks the endpoint as Disabled.
        /// Otherwise, tells the transport to change the number of worker threads.
        /// </summary>
        /// <param name="message"></param>
        public void Handle(ChangeNumberOfWorkerThreadsMessage message)
        {
            int target = message.NumberOfWorkerThreads;
            if (target <= 0)
                target = 1;
            else
            {
                GridInterceptingMessageHandler.Disabled = false;
                this.ReadyManager.ContinueSendingReadyMessages();
            }

            this.Transport.ChangeNumberOfWorkerThreads(target);

            if (message.NumberOfWorkerThreads == 0)
            {
                this.ReadyManager.StopSendingReadyMessages();
                GridInterceptingMessageHandler.Disabled = true;

                logger.Info("Disabling this endpoint.");
            }
            else 
                logger.Info(string.Format("{0} worker threads now running.", target));
        }

        /// <summary>
        /// Used to stop sending ready messages to the distributor if one is configured.
        /// </summary>
        public IManageReadyMessages ReadyManager { get; set; }

        /// <summary>
        /// This is kept separate from the bus because the distributor
        /// will be using this class on its control bus to change the
        /// number of worker threads on its data bus.
        /// 
        /// For regular cases, the transport should be the same as is
        /// configured for the bus.
        /// </summary>
        public ITransport Transport { get; set; }

        private static readonly ILog logger = LogManager.GetLogger("NServicebus.Grid");
    }
}
