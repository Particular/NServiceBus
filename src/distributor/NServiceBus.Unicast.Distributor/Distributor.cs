using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Unicast.Transport;
using Common.Logging;
using System.Threading;

namespace NServiceBus.Unicast.Distributor
{
	/// <summary>
	/// Provides functionality for distributing messages from a bus
	/// to multiple workers when using a unicast transport.
	/// </summary>
    public class Distributor
    {
        #region config info

		/// <summary>
		/// Sets the bus that will be used
		/// for transporting control information.
		/// </summary>
        public virtual IStartableBus ControlBus { get; set; }

		/// <summary>
		/// Sets the transport that will be used
		/// to access the bus containing messages to distribute.
		/// </summary>
        public virtual ITransport MessageBusTransport { get; set; }

		/// <summary>
		/// Sets the <see cref="IWorkerAvailabilityManager"/> implementation that will be
		/// used to determine whether or not a worker is available.
		/// </summary>
        public virtual IWorkerAvailabilityManager WorkerManager { get; set; }


	    private int millisToWaitIfCannotDispatchToWorker = 50;

        /// <summary>
        /// Milliseconds to sleep if no workers are available.
        /// Prevents needless CPU churn.
        /// </summary>
        public virtual int MillisToWaitIfCannotDispatchToWorker
	    {
            set { this.millisToWaitIfCannotDispatchToWorker = value; }
	    }

        #endregion

        #region public methods

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

            this.MessageBusTransport.TransportMessageReceived += messageBusTransport_TransportMessageReceived;

            this.ControlBus.Start();
            this.MessageBusTransport.Start();
            this.WorkerManager.Start();
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
		/// Handles reciept of a message on the bus to distribute for.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <remarks>
		/// This method checks whether a worker is available to handle the message and
		/// forwards it if one is found.
		/// </remarks>
        void messageBusTransport_TransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            if (disabled)
                this.Rollback();

 		    string destination = this.WorkerManager.PopAvailableWorker();

            if (destination == null)
                this.Rollback();
            else
            {
                logger.Debug("Sending message to: " + destination);
                this.MessageBusTransport.Send(e.Message, destination);
            }
        }

        /// <summary>
        /// Rolls back the message that arrived on the MessageBusTransport.
        /// </summary>
        private void Rollback()
        {
            Thread.Sleep(this.millisToWaitIfCannotDispatchToWorker);

            this.MessageBusTransport.AbortHandlingCurrentMessage();
        }

        #endregion

        #region members

	    private volatile bool disabled;
        private readonly static ILog logger = LogManager.GetLogger(typeof(Distributor));

        #endregion
    }
}
