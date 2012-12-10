namespace MyRequestResponseEndpoint
{
    using System;
    using NServiceBus;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization
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
            Console.Out.WriteLine("The MyRequestResponseEndpoint is now started and ready to accept messages");
        }

        public void Stop()
        {
            
        }
    }
}
