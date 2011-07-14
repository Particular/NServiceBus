using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using NServiceBus.Installation;
using NServiceBus.Utils;

namespace NServiceBus.Faults.Forwarder.Config
{
    class Installer : INeedToInstallSomething<Installation.Environments.Windows>
    {
        public void Install(WindowsIdentity identity)
        {
            MsmqUtilities.CreateQueueIfNecessary(ConfigureFaultsForwarder.ErrorQueue, identity.Name);
        }
    }
}
