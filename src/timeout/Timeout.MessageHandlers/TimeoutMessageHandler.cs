using NServiceBus;
using NServiceBus.Saga;

namespace Timeout.MessageHandlers
{
    public class TimeoutMessageHandler : BaseMessageHandler<TimeoutMessage>
    {
        public override void Handle(TimeoutMessage message)
        {
            if (message.HasNotExpired())
                this.Bus.HandleCurrentMessageLater();
            else
                this.Bus.Send(this.Bus.SourceOfMessageBeingHandled, message);
        }
    }
}
