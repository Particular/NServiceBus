using NServiceBus;
using NServiceBus.Config;
using NServiceBus.Config.ConfigurationSource;

namespace Subscriber1
{
    class EndpointConfig : IConfigureThisEndpoint, AsA_Server {}

    //demonstrate how to override specific configuration sections
    class D : IProvideConfiguration<MsmqTransportConfig>
    {
        public MsmqTransportConfig GetConfiguration()
        {
            return new MsmqTransportConfig
                       {
                           ErrorQueue = "error",
                           InputQueue = "Subscriber1InputQueue",
                           MaxRetries = 5,
                           NumberOfWorkerThreads = 1
                       };
        }
    }
}
