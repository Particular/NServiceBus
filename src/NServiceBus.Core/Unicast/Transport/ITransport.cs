namespace NServiceBus.Unicast.Transport
{
    using System;

    /// <summary>
	/// Defines the basic functionality of a transport to be used by NServiceBus.
	/// </summary>
    public interface ITransport : IDisposable
    {
		/// <summary>
		/// Starts the transport.
		/// </summary>
        /// <param name="inputqueue">
		/// The adress of a local queue that should be used as input channel for this transport
        /// </param>
        [ObsoleteEx(Replacement = "Start(Address localAddress)", RemoveInVersion = "4.0", TreatAsErrorFromVersion = "3.4")]		
        void Start(string inputqueue);

        /// <summary>
        /// Starts the transport listening for messages on the given local address.
        /// </summary>
        /// <param name="localAddress"></param>
	    void Start(Address localAddress);

        /// <summary>
        /// Gets the number of worker threads currently running in the transport.
        /// </summary>
        int NumberOfWorkerThreads { get; }

        /// <summary>
        /// Changes the number of worker threads running in the transport.
        /// This may stop active worker threads; those threads will finish
        /// processing their current message and then exit.
        /// </summary>
        /// <param name="targetNumberOfWorkerThreads">
        /// The requested number of active worker threads after
        /// the necessary threads have been stopped or started.
        /// </param>
	    void ChangeNumberOfWorkerThreads(int targetNumberOfWorkerThreads);

        /// <summary>
        /// Throttling receiving messages rate. You can't set the value than the value specified at your license.
        /// </summary>
        int MaxThroughputPerSecond { get; set; }

		/// <summary>
		/// Raised when a message is received.
		/// </summary>
        event EventHandler<TransportMessageReceivedEventArgs> TransportMessageReceived;

        /// <summary>
        /// Raised when a message is available but before <see cref="TransportMessageReceived"/> is raised.
        /// </summary>
        event EventHandler<StartedMessageProcessingEventArgs> StartedMessageProcessing;

        /// <summary>
        /// Raised after message processing was completed, even in case of an exception in message processing.
        /// </summary>
        event EventHandler FinishedMessageProcessing;

        /// <summary>
        /// Raised if an exception was encountered at any point in the processing - including
        /// when the transaction commits.
        /// </summary>
        event EventHandler<FailedMessageProcessingEventArgs> FailedMessageProcessing;

        /// <summary>
        /// Causes the current message being handled to return to the queue.
        /// </summary>
	    void AbortHandlingCurrentMessage();
    }
}
