using Messages;
using NServiceBus;

namespace Server
{
    public class CommandMessageHandler : IMessageHandler<Command>
    {
        public IBus Bus { get; set; }

        public void Handle(Command message)
        {
            if (message.Id % 2 == 0)
                this.Bus.Return((int)ErrorCodes.Fail);
            else 
                this.Bus.Return((int)ErrorCodes.None);
        }
    }
}
