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

        private IBus controlBus;

		/// <summary>
		/// Sets the <see cref="IBus"/> implementation that will be used
		/// for transporting control information.
		/// </summary>
        public virtual IBus ControlBus
        {
            set { controlBus = value; }
        }

        private ITransport messageBusTransport;

		/// <summary>
		/// Sets the <see cref="ITransport"/> implementation that will be used
		/// to access the bus containing messages to distribute.
		/// </summary>
        public virtual ITransport MessageBusTransport
        {
            set { messageBusTransport = value; }
        }

        private IWorkerAvailabilityManager workerManager;

		/// <summary>
		/// Sets the <see cref="IWorkerAvailabilityManager"/> implementation that will be
		/// used to determine whether or not a worker is available.
		/// </summary>
        public virtual IWorkerAvailabilityManager WorkerManager
        {
            set { workerManager = value; }
        }

	    private int millisToWaitIfCannotDispatchToWorker = 50;
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

            this.messageBusTransport.TransportMessageReceived += messageBusTransport_TransportMessageReceived;

            this.controlBus.Start();
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

 		    string destination = this.workerManager.PopAvailableWorker();

            if (destination == null)
                this.Rollback();
            else
            {
                logger.Debug("Sending message to: " + destination);
                this.messageBusTransport.Send(e.Message, destination);
            }
        }

        private void Rollback()
        {
            Thread.Sleep(this.millisToWaitIfCannotDispatchToWorker);

            this.messageBusTransport.AbortHandlingCurrentMessage();
        }

        #endregion

        #region members

	    private volatile bool disabled;
        private readonly static ILog logger = LogManager.GetLogger(typeof(Distributor));

        #endregion
    }
}
