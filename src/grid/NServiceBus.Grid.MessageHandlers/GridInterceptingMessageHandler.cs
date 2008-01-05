

using System;
using NServiceBus.Grid.Messages;

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
            if (message is GetNumberOfWorkerThreadsMessage)
                return;
            if (message is ChangeNumberOfWorkerThreadsMessage)
                return;

            if (disabled)
                this.Bus.DoNotContinueDispatchingCurrentMessageToHandlers();
        }
    }
}
