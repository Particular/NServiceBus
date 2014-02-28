namespace NServiceBus.Distributor.MSMQ.Config
{
    using NServiceBus.Config;
    using ReadyMessages;
    using Settings;

    internal class WorkerInitializer
    {
        public static void Init()
        {
            var masterNodeAddress = Configure.Instance.GetMasterNodeAddress();
            var distributorControlAddress = masterNodeAddress.SubScope("distributor.control");

            var unicastBusConfig = Configure.GetConfigSection<UnicastBusConfig>();

            //allow users to override control address in config
            if (unicastBusConfig != null && !string.IsNullOrWhiteSpace(unicastBusConfig.DistributorControlAddress))
            {
                distributorControlAddress = Address.Parse(unicastBusConfig.DistributorControlAddress);
            }

            Configure.Instance.Configurer.ConfigureComponent<ReadyMessageSender>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.DistributorControlAddress, distributorControlAddress);

            Address.OverridePublicReturnAddress(distributorControlAddress);

            Configure.Instance.Configurer.ConfigureComponent<ReturnAddressRewriter>(
                DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.DistributorDataAddress, masterNodeAddress);

            SettingsHolder.Set("Worker.Enabled", true);
            SettingsHolder.Set("Distributor.Version", 2);
            SettingsHolder.Set("MasterNode.Address", masterNodeAddress);
        }
    }
}