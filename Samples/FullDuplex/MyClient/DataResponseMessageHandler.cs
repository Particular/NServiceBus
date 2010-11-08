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
    
    public class PreventSubscription : IWantCustomInitialization
    {
        public void Init()
        {
            //so we don't end up subscribing to DataResponseMessage
            Configure.Instance.UnicastBus().DoNotAutoSubscribe();
        }
    }
}
