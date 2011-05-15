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

            var bus = NServiceBus.Configure.Instance.Builder.Build<UnicastBus>();

            MsmqUtilities.CreateQueueIfNecessary(Address.Local.ToString(), identity.Name);
        }
    }
}
