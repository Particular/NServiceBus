namespace NServiceBus.Satellites
{
    using System.Linq;
    using Installation;
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

            if (ConfigureQueueCreation.DontCreateQueues)
            {
                return;
            }

            var satellites = config.Builder
                 .BuildAll<ISatellite>()
                 .ToList();

            foreach (var satellite in satellites.Where(satellite => !satellite.Disabled && satellite.InputAddress != null))
            {
                QueueCreator.CreateQueueIfNecessary(satellite.InputAddress, identity);
            }
        }
    }
}
