namespace MyServer
{
    using System;
    using NServiceBus;
    using NServiceBus.ActiveMQ;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Publisher, UsingTransport<ActiveMQ>
    {
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
