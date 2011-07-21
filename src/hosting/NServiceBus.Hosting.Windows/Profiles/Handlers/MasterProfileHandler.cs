using System;
using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    class MasterProfileHandler : IHandleProfile<Master>
    {
        public void ProfileActivated()
        {
            Configure.Instance.AsMasterNode();
        }
    }

    class DynamicDiscoveryHandler : IHandleProfile<DynamicDiscovery>
    {
        public void ProfileActivated()
        {
            Configure.Instance.DynamicNodeDiscovery().UnicastBus().AllowDiscovery();
        }
    }
}
