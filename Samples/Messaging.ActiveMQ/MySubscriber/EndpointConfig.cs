namespace MySubscriber
{
    using System;
    using NServiceBus;
    using NServiceBus.ActiveMQ;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, UsingTransport<ActiveMQ>{}

    public class MyClass:IWantToRunWhenBusStartsAndStops
    {
        public void Start()
        {
            Console.Out.WriteLine("The MySubscriber endpoint is now started and subscribed to events from MyServer");
        }

        public void Stop()
        {
            
        }
    }
}
