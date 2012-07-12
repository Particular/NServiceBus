using System;
using System.Security.Principal;
using NServiceBus.Config;

namespace NServiceBus.Unicast.Queuing.Installers
{
    public class EndpointInputQueueCreator : IWantQueuesCreated<Installation.Environments.Windows>
    {
        public static bool Enabled { get; set; }
        public ICreateQueues QueueCreator { get; set; }

        public void Create(WindowsIdentity identity)
        {
            if (!Enabled)
                return;

            if (QueueCreator == null)
            {
                throw new Exception("No QueueCreator is configured and the EndpointInputQueueCreator is enabled!");
            }

            QueueCreator.CreateQueueIfNecessary(Address.Local, identity.Name, ConfigureVolatileQueues.IsVolatileQueues);

            var unicastConfig = Configure.GetConfigSection<UnicastBusConfig>();

            if (unicastConfig == null) 
                return;

            if (!string.IsNullOrEmpty(unicastConfig.ForwardReceivedMessagesTo))
            {
                QueueCreator.CreateQueueIfNecessary(Address.Parse(unicastConfig.ForwardReceivedMessagesTo), identity.Name, ConfigureVolatileQueues.IsVolatileQueues);
            }
        }
    }
}
