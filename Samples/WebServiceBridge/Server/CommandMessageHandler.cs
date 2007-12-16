using Messages;
using NServiceBus;

namespace Server
{
    public class CommandMessageHandler : BaseMessageHandler<Command>
    {
        public override void Handle(Command message)
        {
            if (message.Id % 2 == 0)
                this.Bus.Return((int)ErrorCodes.Fail);
            else 
                this.Bus.Return((int)ErrorCodes.None);
        }
    }
}
