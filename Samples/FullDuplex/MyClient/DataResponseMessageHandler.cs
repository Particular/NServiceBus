using System;
using MyMessages;
using NServiceBus;

namespace MyClient
{
    class DataResponseMessageHandler : IHandleMessages<DataResponseMessage>
    {
        public void Handle(DataResponseMessage message)
        {
            Console.WriteLine("Response received with description: {0}\nAnswer: {1}",
                message.String, message.Answer);
        }
    }

    class NeutralizeSubscriptions : IWantCustomInitialization
    {
        void IWantCustomInitialization.Init()
        {
            //so that we don't ask to subscribe to events because server isn't a publisher
            NServiceBus.Configure.Instance.UnicastBus().DoNotAutoSubscribe();
        }
    }
}
