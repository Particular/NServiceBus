using System;

namespace NServiceBus.Unicast.Transport
{
	/// <summary>
	/// Defines the basic functionality of a transport to be used by NServiceBus.
	/// </summary>
    public interface ITransport : IDisposable
    {
		/// <summary>
		/// Starts the transport.
		/// </summary>
        void Start();

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
		/// Raised when a message is received.
		/// </summary>
        event EventHandler<TransportMessageReceivedEventArgs> TransportMessageReceived;

        /// <summary>
        /// Raised when a message is available but before <see cref="TransportMessageReceived"/> is raised.
        /// </summary>
	    event EventHandler StartedMessageProcessing;

        /// <summary>
        /// Raised after message processing was completed, even in case of an exception in message processing.
        /// </summary>
        event EventHandler FinishedMessageProcessing;

        /// <summary>
        /// Raised if an exception was encountered at any point in the processing - including
        /// when the transaction commits.
        /// </summary>
	    event EventHandler FailedMessageProcessing;

        /// <summary>
        /// Causes the current message being handled to return to the queue.
        /// </summary>
	    void AbortHandlingCurrentMessage();
    }
}
