using log4net;
using NServiceBus.Messages;

namespace NServiceBus.Unicast.Distributor
{
    /// <summary>
    /// Handles <see cref="ReadyMessage"/> and updates the worker availability manager.
    /// </summary>
    public class ReadyMessageHandler : IMessageHandler<ReadyMessage>
    {
        /// <summary>
        /// Handles the message.
        /// </summary>
        /// <param name="message"></param>
        public void Handle(ReadyMessage message)
        {
            logger.Debug("Server available: " + Bus.CurrentMessageContext.ReturnAddress);

            if (message.ClearPreviousFromThisAddress) //indicates worker started up
                WorkerManager.ClearAvailabilityForWorker(Bus.CurrentMessageContext.ReturnAddress);

            WorkerManager.WorkerAvailable(Bus.CurrentMessageContext.ReturnAddress);
        }

        /// <summary>
        /// Used to get the address of the endpoint that sent the message
        /// </summary>
        public virtual IBus Bus { get; set; }

        /// <summary>
        /// Updated based on the message state.
        /// </summary>
        public virtual IWorkerAvailabilityManager WorkerManager { get; set; }


        private readonly static ILog logger = LogManager.GetLogger(typeof(ReadyMessageHandler));
    }
}
