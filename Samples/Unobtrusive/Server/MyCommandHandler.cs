namespace Server
{
    using System;
    using Commands;
    using NServiceBus;

    public class MyCommandHandler:IHandleMessages<MyCommand>
    {
        readonly IBus bus;

        public MyCommandHandler(IBus bus)
        {
            this.bus = bus;
        }

        public void Handle(MyCommand message)
        {
            Console.WriteLine("Command received, data:" + message.CommandId);

            bus.Return(CommandStatus.Ok);
        }
    }
}