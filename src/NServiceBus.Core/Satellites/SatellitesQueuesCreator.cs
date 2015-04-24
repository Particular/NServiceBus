
namespace NServiceBus.Satellites
{
    using Installation;
    using NServiceBus.Pipeline;
    using Transports;

    class SatellitesQueuesCreator : INeedToInstallSomething
    {
        public ICreateQueues QueueCreator { get; set; }

        public void Install(string identity, Configure config)
        {
            if (config.Settings.Get<bool>("Endpoint.SendOnly"))
            {
                return;
            }

            if (!config.CreateQueues())
            {
                return;
            }

            var satellites = config.Settings.Get<PipelinesCollection>().SatellitePipelines;

            foreach (var satellite in satellites)
            {
                QueueCreator.CreateQueueIfNecessary(satellite.ReceiveAddress, identity);
            }
        }
    }
}