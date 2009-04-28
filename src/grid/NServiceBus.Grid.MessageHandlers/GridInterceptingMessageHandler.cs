

using System;
using Common.Logging;
using NServiceBus.Grid.Messages;
using NServiceBus.Unicast;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Grid.MessageHandlers
{
    /// <summary>
    /// Intercepts all messages, not allowing any through if the endpoint
    /// has had its number of worker threads reduced to zero.
    /// </summary>
    public class GridInterceptingMessageHandler : IMessageHandler<IMessage>
    {
        /// <summary>
        /// The bus instructed to stop messages from being processed further. 
        /// </summary>
        public IBus Bus { get; set; }

        /// <summary>
        /// Used to prevent ready messages from being sent to the distributor
        /// when grid messages are processed.
        /// </summary>
        public IUnicastBus UnicastBus { get; set; }

        /// <summary>
        /// Used to abort handling messages when the endpoint has been disabled.
        /// </summary>
        public ITransport Transport { get; set; }

        private static volatile bool disabled;

        /// <summary>
        /// Gets/sets that the number of worker threads has been reduced to zero.
        /// </summary>
        public static bool Disabled
        {
            get
            {
                return disabled;
            }
            set
            {
                if (disabled != value)
                {
                    disabled = value;

                    if (DisabledChanged != null)
                        DisabledChanged(null, null);
                }
            }
        }

        /// <summary>
        /// Notifies when the Disabled state has changed.
        /// </summary>
        public static event EventHandler DisabledChanged;

        /// <summary>
        /// If Disabled, does not allow the message to be processed (unless it is a grid message).
        /// Prevents grid messages from causing the bus to send ready messages to the distributor (if such is configured).
        /// </summary>
        /// <param name="message"></param>
        public void Handle(IMessage message)
        {
            if (message is GetNumberOfWorkerThreadsMessage ||
                message is ChangeNumberOfWorkerThreadsMessage ||
                message is GotNumberOfWorkerThreadsMessage)
            {
                this.UnicastBus.SkipSendingReadyMessageOnce();
                return;
            }

            if (disabled)
            {
                this.Bus.DoNotContinueDispatchingCurrentMessageToHandlers();
                this.Transport.AbortHandlingCurrentMessage();

                logger.Info("Endpoint is currently disabled. Send a 'ChangeNumberOfWorkerThreadsMessage' to change this.");
            }
        }

        private static readonly ILog logger = LogManager.GetLogger("NServicebus.Grid");
    }
}
