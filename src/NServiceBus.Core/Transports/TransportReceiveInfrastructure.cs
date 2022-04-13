namespace NServiceBus.Transport
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents the infrastructure of the transport used for receiving.
    /// </summary>
    public class TransportReceiveInfrastructure
    {
        /// <summary>
        /// Creates new result.
        /// </summary>
        /// <param name="messagePumpFactory">Factory for creating a message pump for each queue to be consumed from.</param>
        /// <param name="queueCreatorFactory">Factory for the queue creator.</param>
        /// <param name="preStartupCheck">Callback to perform checks before the transport starts receiving messages.</param>
        public TransportReceiveInfrastructure(
            Func<IPushMessages> messagePumpFactory,
            Func<ICreateQueues> queueCreatorFactory,
            Func<Task<StartupCheckResult>> preStartupCheck)
        {
            Guard.AgainstNull(nameof(messagePumpFactory), messagePumpFactory);
            Guard.AgainstNull(nameof(queueCreatorFactory), queueCreatorFactory);
            Guard.AgainstNull(nameof(preStartupCheck), preStartupCheck);

            MessagePumpFactory = messagePumpFactory;
            QueueCreatorFactory = queueCreatorFactory;
            PreStartupCheck = preStartupCheck;
        }

        /// <summary>
        /// Factory for creating a message pump for each queue to be consumed from.
        /// </summary>
        public Func<IPushMessages> MessagePumpFactory { get; }

        /// <summary>
        /// Factory for the queue creator.
        /// </summary>
        public Func<ICreateQueues> QueueCreatorFactory { get; }

        /// <summary>
        /// Callback to perform checks before the transports starts receiving messages.
        /// </summary>
        /// <remarks>Queue creation happens before the check is executed.</remarks>
        public Func<Task<StartupCheckResult>> PreStartupCheck { get; }
    }
}
