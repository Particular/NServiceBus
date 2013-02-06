namespace MySubscriber
{
    using System;
    using NServiceBus;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Publisher, UsingTransport<Msmq> { }

    public class MyClass : IWantToRunWhenBusStartsAndStops
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
