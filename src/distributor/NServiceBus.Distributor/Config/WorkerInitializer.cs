namespace NServiceBus.Distributor.Config
{
    using NServiceBus.Config;
    using ReadyMessages;

    public class WorkerInitializer:INeedInitialization
    {
        public void Init()
        {
            if (!Configure.Instance.DistributorEnabled())
                return;

            var config = Configure.Instance;

            var masterNodeAddress = config.GetMasterNodeAddress();

            var distributorControlAddress = masterNodeAddress.SubScope("distributor.control");


            config.Configurer.ConfigureComponent<ReadyMessageSender>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.DistributorControlAddress, distributorControlAddress);

            config.Configurer.ConfigureComponent<ReturnAddressRewriter>(
                DependencyLifecycle.SingleInstance)
                .ConfigureProperty(r => r.DistributorDataQueue, masterNodeAddress);
        }
    }
}