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
            Console.Out.WriteLine("MyCommand message received, from {0}", message.Name);

            //send out a request (a event will be published when the response comes back)
            Bus.Send<MyRequest>(r => r.RequestData = string.Format("Send a present to {0}", message.Name));

            //tell the client that we accepted the command
            Bus.Return(CommandStatus.Ok);
        }
    }
}