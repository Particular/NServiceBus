namespace MyServer
{
    using System;
    using MyMessages.Commands;
    using MyMessages.RequestResponse;
    using NServiceBus;

    public class MyCommandHandler : IHandleMessages<MyCommand>
    {
        public IBus Bus { get; set; }

        public void Handle(MyCommand message)
        {
            Console.Out.WriteLine("MyCommand message received, Description: " + message.Description);

            //send out a request (a event will be pushlished when the response comes back)
            Bus.Send<MyRequest>(r => r.RequestData = "The server is making a request");

            //tell the client that we accepted the command
            Bus.Return(CommandStatus.Ok);
        }
    }
}