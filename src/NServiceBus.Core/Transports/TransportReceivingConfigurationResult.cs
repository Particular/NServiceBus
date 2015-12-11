namespace NServiceBus.Transports
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents the result of configuring the transport for receiving.
    /// </summary>
    public class TransportReceivingConfigurationResult
    {
        /// <summary>
        /// Creates new result.
        /// </summary>
        public TransportReceivingConfigurationResult(
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

        internal Func<IPushMessages> MessagePumpFactory { get; }
        internal Func<ICreateQueues> QueueCreatorFactory { get; }
        internal Func<Task<StartupCheckResult>> PreStartupCheck { get; } 

    }
}