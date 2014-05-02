namespace NServiceBus.Unicast.Transport
{
    using System;

    /// <summary>
	/// Defines the basic functionality of a transport to be used by NServiceBus.
	/// </summary>
	public interface ITransport
	{

		/// <summary>
		/// Starts the transport listening for messages on the given local address.
		/// </summary>
		void Start(Address localAddress);

		/// <summary>
		/// Gets the maximum concurrency level this <see cref="ITransport"/> is able to support.
		/// </summary>
		int MaximumConcurrencyLevel { get; }
        
		/// <summary>
		/// Updates the maximum concurrency level this <see cref="ITransport"/> is able to support.
		/// </summary>
		/// <param name="maximumConcurrencyLevel">The new maximum concurrency level for this <see cref="ITransport"/>.</param>
		void ChangeMaximumConcurrencyLevel(int maximumConcurrencyLevel);

		/// <summary>
		/// Throttling receiving messages rate. You can't set the value than the value specified at your license.
		/// </summary>
		[ObsoleteEx(Replacement = "MaximumMessageThroughputPerSecond and ChangeMaximumMessageThroughputPerSecond(int maximumMessageThroughputPerSecond)", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
		int MaxThroughputPerSecond { get; set; }

		/// <summary>
		/// Gets the receiving messages rate.
		/// </summary>
		int MaximumMessageThroughputPerSecond { get; }

		/// <summary>
		/// Updates the max throughput per second.
		/// </summary>
		/// <param name="maximumMessageThroughputPerSecond">The new max throughput.</param>
		void ChangeMaximumMessageThroughputPerSecond(int maximumMessageThroughputPerSecond);

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
        event EventHandler<FinishedMessageProcessingEventArgs> FinishedMessageProcessing;

		/// <summary>
		/// Raised if an exception was encountered at any point in the processing - including
		/// when the transaction commits.
		/// </summary>
		event EventHandler<FailedMessageProcessingEventArgs> FailedMessageProcessing;

		/// <summary>
		/// Causes the current message being handled to return to the queue.
		/// </summary>
		void AbortHandlingCurrentMessage();

        /// <summary>
        /// Stops the transport.
        /// </summary>
	    void Stop();
	}
}
