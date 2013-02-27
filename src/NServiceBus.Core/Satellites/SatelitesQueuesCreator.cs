namespace NServiceBus.Satellites
{
    using System.Linq;
    using Installation;
    using Installation.Environments;
    using Settings;
    using Transports;

    /// <summary>
    /// Responsible to create a queue, using the registered ICreateQueues for each satellite
    /// </summary>
    public class SatelitesQueuesCreator : INeedToInstallSomething<Windows>
    {
        public ICreateQueues QueueCreator { get; set; }
        
        /// <summary>
        /// Performs the installation providing permission for the given user.
        /// </summary>
        /// <param name="identity">The user for whom permissions will be given.</param>
        public void Install(string identity)
        {
            if (SettingsHolder.Get<bool>("Endpoint.SendOnly"))
            {
                return;
            }

            if (ConfigureQueueCreation.DontCreateQueues)
            {
                return;
            }

            var satellites = Configure.Instance.Builder
                 .BuildAll<ISatellite>()
                 .ToList();

            foreach (var satellite in satellites.Where(satellite => !satellite.Disabled && satellite.InputAddress != null))
            {
                QueueCreator.CreateQueueIfNecessary(satellite.InputAddress, identity);
            }
        }
    }
}
