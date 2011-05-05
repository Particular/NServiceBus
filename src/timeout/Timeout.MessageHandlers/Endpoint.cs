using System;
using System.Configuration;
using System.Security.Principal;
using NServiceBus;
using NServiceBus.Installation;
using NServiceBus.ObjectBuilder;
using NServiceBus.Utils;
using Configure = NServiceBus.Configure;

namespace Timeout.MessageHandlers
{
    /// <summary>
    /// Configures the timeout host.
    /// </summary>
    public class Endpoint : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization
    {
        void IWantCustomInitialization.Init()
        {
            var configure = NServiceBus.Configure.With().DefaultBuilder();

            string nameSpace = ConfigurationManager.AppSettings["NameSpace"];
            string serialization = ConfigurationManager.AppSettings["Serialization"];

            switch (serialization)
            {
                case "xml":
                    configure.XmlSerializer(nameSpace);
                    break;
                case "binary":
                    configure.BinarySerializer();
                    break;
                default:
                    throw new ConfigurationErrorsException("Serialization can only be one of 'interfaces', 'xml', or 'binary'.");
            }

            configure.Configurer.ConfigureComponent<TimeoutManager>(ComponentCallModelEnum.Singleton);
            configure.Configurer.ConfigureComponent<TimeoutPersister>(ComponentCallModelEnum.Singleton)
                .ConfigureProperty(tp => tp.Queue, "timeout.storage");
        }
    }

    public class Installer : INeedToInstallSomething<NServiceBus.Installation.Environments.Windows>
    {
        public void Install(WindowsIdentity identity)
        {
            MsmqUtilities.CreateQueueIfNecessary("timeout.storage", identity.Name);
        }
    }
}
