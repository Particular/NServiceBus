using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using NServiceBus.Installation;
using NServiceBus.Utils;

namespace NServiceBus.Unicast.Queuing.Msmq.Config
{
    class Installer : INeedToInstallSomething<Installation.Environments.Windows>
    {
        public void Install(WindowsIdentity identity)
        {
            if (!ConfigureMsmqMessageQueue.Selected)
                return;

            MsmqUtilities.CreateQueueIfNecessary(Address.Local, identity.Name);
        }
    }
}
