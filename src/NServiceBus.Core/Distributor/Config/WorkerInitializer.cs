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

            //allow users to override controll address in config
            if (unicastBusConfig != null && !string.IsNullOrWhiteSpace(unicastBusConfig.DistributorControlAddress))
                distributorControlAddress = Address.Parse(unicastBusConfig.DistributorControlAddress);

            config.Configurer.ConfigureComponent<ReadyMessageSender>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.DistributorControlAddress, distributorControlAddress);


            var distributorDataAddress = masterNodeAddress;
           
            //allow users to override data address in config
            if (unicastBusConfig != null && !string.IsNullOrWhiteSpace(unicastBusConfig.DistributorDataAddress))
                distributorDataAddress = Address.Parse(unicastBusConfig.DistributorDataAddress);


           
            config.Configurer.ConfigureComponent<ReturnAddressRewriter>(
                DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.DistributorDataAddress, distributorDataAddress);
        }
    }
}