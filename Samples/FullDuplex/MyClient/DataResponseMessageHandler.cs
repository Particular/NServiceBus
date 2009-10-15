using System;
using MyMessages;
using NServiceBus;

namespace Client
{
    class DataResponseMessageHandler : IHandleMessages<DataResponseMessage>
    {
        public void Handle(DataResponseMessage message)
        {
            Console.WriteLine("Response received with description: {0}\nSecret answer: {1}",
                message.String, message.SecretAnswer.Value);
        }
    }
}
