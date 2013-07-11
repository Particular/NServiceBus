using Messages;
using NServiceBus;

namespace Worker.MessageHandlers
{
    public class SayHello : IHandleMessages<SayHelloTo>
    {
        private readonly IBus bus;

        public SayHello(IBus bus)
        {
            this.bus = bus;
        }

        public void Handle(SayHelloTo message)
        {
            var hello = bus.CreateInstance<Hello>();
            hello.Text = "Hello " + message.Name + " !!!!!!!!!!!!!!!!!!!!";
            bus.Reply(hello);
        }
    }
}
