namespace MyServer
{
    using System;
    using NServiceBus;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Publisher, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                     .DefaultBuilder()
                     .XmlSerializer(dontWrapSingleMessages: true)
                     .ActiveMqTransport();
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
