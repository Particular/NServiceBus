namespace VideoStore.CustomerRelations
{
    using System;
    using NServiceBus;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Publisher, UsingTransport<Msmq> { }

    public class MyClass : IWantToRunWhenBusStartsAndStops
    {
        public void Start()
        {
            Console.Out.WriteLine("The VideoStore.CustomerRelations endpoint is now started and subscribed to events from VideoStore.Sales");
        }

        public void Stop()
        {

        }
    }
}
