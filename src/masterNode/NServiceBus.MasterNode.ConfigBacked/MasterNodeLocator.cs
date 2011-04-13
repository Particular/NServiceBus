using System;
using NServiceBus.Config;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.MasterNode.ConfigBacked
{
    public class MasterNodeLocator : ILocateMasterNode
    {
        public string GetMasterNode()
        {
            var section = Configure.GetConfigSection<MasterNodeLocatorConfig>();
            if (section != null)
                return section.Node;

            return null;
        }
    }

    public class Bootstrapper : INeedInitialization
    {
        public void Init()
        {
            if (!Configure.Instance.Configurer.HasComponent<ILocateMasterNode>())
                Configure.Instance.Configurer.ConfigureComponent<MasterNodeLocator>(DependencyLifecycle.SingleInstance);
        }
    }
}
