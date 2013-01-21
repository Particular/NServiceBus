namespace MyServer
{
    using System;
    using NServiceBus;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Publisher, UsingTransport<Msmq>, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                     .DefaultBuilder()
                     .UnicastBus()
                        .DoNotAutoSubscribe();
        }
    }


    public class MyClass:IWantToRunWhenBusStartsAndStops
    {
        public void Start()
        {
            Console.Out.WriteLine("The MyServer endpoint is now started and ready to accept messages");
        }

        public void Stop()
        {
            
        }
    }
}
