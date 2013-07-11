using System;
using NServiceBus;
using NServiceBus.Faults;

namespace MyServerWithSLR
{
    public class MyOwnFaultManager : IManageMessageFailures, INeedInitialization
    {
        public void SerializationFailedForMessage(TransportMessage message, Exception e)
        {
            LogMessage("SerializationFailedForMessage");
        }

        public void ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
        {
            LogMessage("ProcessingAlwaysFailsForMessage");
            throw new Exception("Failure in the MyOwnFaultManager");
        }

        public void Init(Address address)
        {
            faultManagerFor = address;
            LogMessage("Init");
        }

        public void Init()
        {
            // Enable this if you would like to use your own Fault Manager, this
            // will also disable SLR
            //Configure.Instance.Configurer.ConfigureComponent<MyOwnFaultManager>(DependencyLifecycle.InstancePerCall);
        }

        void LogMessage(string message)
        {
            Console.WriteLine(string.Format("MyOwnFaultManager(Transport:{1}) - {0}", message, faultManagerFor));
        }

        Address faultManagerFor;
    }
}