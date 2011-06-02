using NServiceBus.Config;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.MasterNode.Discovery
{
    public class Bootstrapper : INeedInitialization
    {
        public void Init()
        {
            if (!Configure.Instance.Configurer.HasComponent<IManageTheMasterNode>())
                Configure.Instance.Configurer.ConfigureComponent<MasterNodeManager>(DependencyLifecycle.SingleInstance);
        }
    }
}
