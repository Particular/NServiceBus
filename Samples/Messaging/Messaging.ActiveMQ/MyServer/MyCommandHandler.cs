namespace MyServer
{
    using System;
    using MyMessages;
    using NServiceBus;

    public class MyCommandHandler:IHandleMessages<MyCommand>
    {
        public void Handle(MyCommand message)
        {
            Console.Out.WriteLine("MyCommand message received, Description: " + message.Description);
        }
    }
}