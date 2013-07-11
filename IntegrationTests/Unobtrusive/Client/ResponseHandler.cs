namespace Client
{
    using System;
    using Messages;
    using NServiceBus;

    public class ResponseHandler : IHandleMessages<Response>
    {
        public void Handle(Response message)
        {
            Console.WriteLine("Response received from server for request with id:" + message.ResponseId);
        }
    }
}