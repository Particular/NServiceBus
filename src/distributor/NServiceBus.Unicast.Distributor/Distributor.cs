using System.Threading;
using log4net;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Unicast.Transport;
using NServiceBus.Unicast.Queuing;

namespace NServiceBus.Unicast.Distributor
{
    /// <summary>
    /// Provides functionality for distributing messages from a bus
    /// to multiple workers when using a unicast transport.
    /// </summary>
    public class Distributor
    {
        private int millisToWaitIfCannotDispatchToWorker = 50;

        /// <summary>
        /// Sets the bus that will be used
        /// for transporting control information.
        /// </summary>
        public IStartableBus ControlBus { get; set; }

        /// <summary>
        /// Sets the transport that will be used
        /// to access the bus containing messages to distribute.
        /// </summary>
        public ITransport MessageBusTransport { get; set; }

        /// <summary>
        /// Object used to send messages.
        /// </summary>
        public ISendMessages MessageSender { get; set; }

        /// <summary>
        /// Sets the <see cref="IWorkerAvailabilityManager"/> implementation that will be
        /// used to determine whether or not a worker is available.
        /// </summary>
        public IWorkerAvailabilityManager WorkerManager { get; set; }


        /// <summary>
        /// Milliseconds to sleep if no workers are available.
        /// Prevents needless CPU churn.
        /// </summary>
        public int MillisToWaitIfCannotDispatchToWorker
        {
            get { return millisToWaitIfCannotDispatchToWorker; }
            set { millisToWaitIfCannotDispatchToWorker = value; }
        }

        /// <summary>
        /// Starts the Distributor.
        /// </summary>
        public void Start()
        {
            GridInterceptingMessageHandler.DisabledChanged +=
                delegate
                    {
                        disabled =
                            GridInterceptingMessageHandler.Disabled;
                    };

            MessageBusTransport.TransportMessageReceived += messageBusTransport_TransportMessageReceived;
            MessageBusTransport.Start();

            WorkerManager.Start();
        }

        /// <summary>
        /// Stops the Distributor.
        /// </summary>
        public void Stop()
        {
            MessageBusTransport.TransportMessageReceived -= messageBusTransport_TransportMessageReceived;
        }

        /// <summary>
        /// Handles reciept of a message on the bus to distribute for.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>
        /// This method checks whether a worker is available to handle the message and
        /// forwards it if one is found.
        /// </remarks>
        private void messageBusTransport_TransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            if (disabled)
                Rollback();

            string destination = WorkerManager.PopAvailableWorker();

            if (destination == null)
                Rollback();
            else
            {
                logger.Debug("Sending message to: " + destination);
                MessageSender.Send(e.Message, destination);
            }
        }

        /// <summary>
        /// Rolls back the message that arrived on the MessageBusTransport.
        /// </summary>
        private void Rollback()
        {
            Thread.Sleep(millisToWaitIfCannotDispatchToWorker);

            MessageBusTransport.AbortHandlingCurrentMessage();
        }

        private static readonly ILog logger = LogManager.GetLogger(typeof (Distributor));
        private volatile bool disabled;
    }
}