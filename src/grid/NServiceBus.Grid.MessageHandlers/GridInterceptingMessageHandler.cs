

using System;
using Common.Logging;
using NServiceBus.Grid.Messages;
using NServiceBus.Unicast;

namespace NServiceBus.Grid.MessageHandlers
{
    public class GridInterceptingMessageHandler : BaseMessageHandler<IMessage>
    {
        private static volatile bool disabled;
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

        public static event EventHandler DisabledChanged;

        public override void Handle(IMessage message)
        {
            if (message is GetNumberOfWorkerThreadsMessage ||
                message is ChangeNumberOfWorkerThreadsMessage ||
                message is GotNumberOfWorkerThreadsMessage)
            {
                this.unicastBus.SkipSendingReadyMessageOnce();
                return;
            }

            if (disabled)
            {
                this.Bus.DoNotContinueDispatchingCurrentMessageToHandlers();

                logger.Info("Endpoint is currently disabled. Send a 'ChangeNumberOfWorkerThreadsMessage' to change this.");
            }
        }

        private IUnicastBus unicastBus;
        public IUnicastBus UnicastBus
        {
            set
            {
                this.unicastBus = value;
            }
        }

        private static readonly ILog logger = LogManager.GetLogger("NServicebus.Grid");

    }
}
