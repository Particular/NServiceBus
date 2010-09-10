using System;
using MyMessages;
using NServiceBus;

namespace MyClient
{
    class DataResponseMessageHandler : IHandleMessages<DataResponseMessage>
    {
        public void Handle(DataResponseMessage message)
        {
            Console.WriteLine("Response received with description: {0}", message.String);
        }
    }

    class DontSubscribe : IWantCustomInitialization
    {
        public void Init()
        {
            // since the server isn't configured to be a publisher, we include this
            // so that we don't subscribe to the DataResponseMessage
            NServiceBus.Configure.Instance.UnicastBus().DoNotAutoSubscribe();
        }
    }
}
