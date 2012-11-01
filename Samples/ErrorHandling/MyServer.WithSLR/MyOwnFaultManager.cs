using System;
using NServiceBus;
using NServiceBus.Config;
using NServiceBus.Faults;
using NServiceBus.Unicast.Transport;

namespace MyServerWithSLR
{
    public class MyOwnFaultManager : IManageMessageFailures, INeedInitialization
    {
        public void SerializationFailedForMessage(TransportMessage message, Exception e)
        {
            Console.WriteLine("MyOwnFaultManager - SerializationFailedForMessage");
        }

        public void ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
        {
            Console.WriteLine("MyOwnFaultManager - ProcessingAlwaysFailsForMessage");
        }

        public void Init(Address address)
        {
            Console.WriteLine("MyOwnFaultManager - Init");
        }

        public void Init()
        {
            // Enable this if you would like to use your own Fault Manager, this
            // will also disable SLR
            
            // Configure.Instance.Configurer.ConfigureComponent<MyOwnFaultManager>(DependencyLifecycle.InstancePerCall);
        }
    }
}