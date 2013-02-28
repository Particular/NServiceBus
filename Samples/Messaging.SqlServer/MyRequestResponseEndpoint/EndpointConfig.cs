namespace MyRequestResponseEndpoint
{
    using System;
    using NServiceBus;

	public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, UsingTransport<SqlServer>, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                     .DefaultBuilder()
                     .UnicastBus()
                     .DoNotAutoSubscribe();
        }
    }
	
    public class MyClass : IWantToRunWhenBusStartsAndStops
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
