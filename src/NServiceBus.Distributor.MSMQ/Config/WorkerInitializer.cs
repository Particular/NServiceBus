namespace NServiceBus.Distributor.MSMQ.Config
{
    using NServiceBus.Config;
    using ReadyMessages;

    internal class WorkerInitializer
    {
        public static void Init()
        {
            var masterNodeAddress = MasterNodeUtils.GetMasterNodeAddress();

            var distributorControlAddress = masterNodeAddress.SubScope("distributor.control");

            var unicastBusConfig = Configure.GetConfigSection<UnicastBusConfig>();

            //allow users to override control address in config
            if (unicastBusConfig != null && !string.IsNullOrWhiteSpace(unicastBusConfig.DistributorControlAddress))
            {
                distributorControlAddress = Address.Parse(unicastBusConfig.DistributorControlAddress);
            }

            Configure.Instance.Configurer.ConfigureComponent<ReadyMessageSender>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.DistributorControlAddress, distributorControlAddress);

            Configure.Instance.Configurer.ConfigureComponent<ReturnAddressRewriter>(
                DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.DistributorDataAddress, masterNodeAddress);
        }
    }
}