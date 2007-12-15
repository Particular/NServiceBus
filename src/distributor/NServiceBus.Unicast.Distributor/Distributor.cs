using System;
using System.Collections.Generic;
using System.Text;
using NServiceBus.Unicast.Transport;
using NServiceBus.Messages;
using System.Threading;
using Common.Logging;

namespace NServiceBus.Unicast.Distributor
{
	/// <summary>
	/// Provides functionality for distributing messages from a bus
	/// to multiple workers when using a unicast transport.
	/// </summary>
    public class Distributor
    {
        #region config info

        private ITransport subscriptionInfoTransport;

		/// <summary>
		/// Sets the <see cref="ITransport"/> implementation that will be used
		/// for transporting subscription information.
		/// </summary>
        public ITransport SubscriptionInfoTransport
        {
            set { subscriptionInfoTransport = value; }
        }

        private ITransport messageBusTransport;

		/// <summary>
		/// Sets the <see cref="ITransport"/> implementation that will be used
		/// to access the bus containing messages to distribute.
		/// </summary>
        public ITransport MessageBusTransport
        {
            set { messageBusTransport = value; }
        }

        private IWorkerAvailabilityManager workerManager;

		/// <summary>
		/// Sets the <see cref="IWorkerAvailabilityManager"/> implementation that will be
		/// used to determine whether or not a worker is available.
		/// </summary>
        public IWorkerAvailabilityManager WorkerManager
        {
            set { workerManager = value; }
        }

        #endregion

        #region public methods

		/// <summary>
		/// Starts the Distributor.
		/// </summary>
        public void Start()
        {
            this.subscriptionInfoTransport.MessageTypesToBeReceived = new List<Type>(new Type[] { typeof(ReadyMessage) });

            this.messageBusTransport.MsgReceived += new EventHandler<MsgReceivedEventArgs>(messageBusTransport_MsgReceived);
            this.subscriptionInfoTransport.MsgReceived += new EventHandler<MsgReceivedEventArgs>(subscriptionInfoTransport_MsgReceived);

            this.subscriptionInfoTransport.Start();
            this.messageBusTransport.Start();
        }

		/// <summary>
		/// Stops the Distributor.
		/// </summary>
        public void Stop()
        {
        }

        #endregion

        #region helper methods

        /// <summary>
        /// Occurs when a <see cref="ReadyMessage"/> arrives on the subscriptionInfoTransport
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>
        /// Stores information about availability of a worker.
        /// May clear previous availability information - in the case
        /// where a worker is just starting up.
        /// </remarks>
        void subscriptionInfoTransport_MsgReceived(object sender, MsgReceivedEventArgs e)
        {
            if (e.Message.Body != null)
                if (e.Message.Body.Length == 1)
                    if (e.Message.Body[0] is ReadyMessage)
                    {
                        ReadyMessage rm = e.Message.Body[0] as ReadyMessage;

                        logger.Debug("Server available: " + e.Message.ReturnAddress);

                        if (rm.ClearPreviousFromThisAddress)
                            this.workerManager.ClearAvailabilityForWorker(e.Message.ReturnAddress);

                        this.workerManager.WorkerAvailable(e.Message.ReturnAddress);
                    }
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
        void messageBusTransport_MsgReceived(object sender, MsgReceivedEventArgs e)
        {
            string destination = this.workerManager.PopAvailableWorker();

            if (destination == null)
                this.messageBusTransport.ReceiveMessageLater(e.Message);
            else
            {
                logger.Debug("Sending message to: " + destination);
                this.messageBusTransport.Send(e.Message, destination);
            }
        }

        #endregion

        #region members

        private static ILog logger = LogManager.GetLogger(typeof(Distributor));

        #endregion
    }
}
