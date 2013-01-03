namespace NServiceBus.Distributor.Config
{
    using NServiceBus.Config;
    using ReadyMessages;

    public class WorkerInitializer
    {
        public static void Init()
        {
            var config = Configure.Instance;

            var masterNodeAddress = config.GetMasterNodeAddress();

            var distributorControlAddress = masterNodeAddress.SubScope("distributor.control");

            var unicastBusConfig = Configure.GetConfigSection<UnicastBusConfig>();

            //allow users to override control address in config
            if (unicastBusConfig != null && !string.IsNullOrWhiteSpace(unicastBusConfig.DistributorControlAddress))
                distributorControlAddress = Address.Parse(unicastBusConfig.DistributorControlAddress);

            config.Configurer.ConfigureComponent<ReadyMessageSender>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.DistributorControlAddress, distributorControlAddress);

            config.Configurer.ConfigureComponent<ReturnAddressRewriter>(
                DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.DistributorDataAddress, masterNodeAddress);
        }
    }
}