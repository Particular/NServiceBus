using System;
using NServiceBus;
using TestMessage;

namespace TestClient
{
    class FtpResponseHandler : IHandleMessages<FtpResponse> 
    {
        public void Handle(FtpResponse message)
        {
            Console.WriteLine("Response received from server for request with id:" + message.ResponseId);
        }
    }
}
