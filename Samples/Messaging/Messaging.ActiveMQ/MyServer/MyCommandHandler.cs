namespace MyServer
{
    using System;
    using MyMessages;
    using NServiceBus;

    public class MyCommandHandler:IHandleMessages<MyCommand>
    {
        public IBus Bus { get; set; }

        public void Handle(MyCommand message)
        {
            Console.Out.WriteLine("MyCommand message received, Description: " + message.Description);

            Bus.Publish(new MyEvent());

            Bus.Return(CommandStatus.Ok);
        }
    }
}