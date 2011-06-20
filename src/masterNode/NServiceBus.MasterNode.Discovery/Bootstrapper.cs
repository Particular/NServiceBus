using System;
using System.Threading;
using Common.Logging;
using NServiceBus.Config;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.MasterNode.Discovery
{
    public class Bootstrapper : INeedInitialization
    {
        public void Init()
        {
            if (RoutingConfig.IsDynamicNodeDiscoveryOn)
            {
                Configure.Instance.Configurer.ConfigureComponent<MasterNodeManager>(DependencyLifecycle.SingleInstance);

                var blocker = new ManualResetEvent(false);
                bool identified = false;
                MasterNodeManager.Init(Address.Local, RoutingConfig.IsConfiguredAsMasterNode, s => { blocker.Set();
                                                                                                       identified = true;
                });

                Logger.Info("Going to search for master node on network - will block for up to 30s.");
                blocker.WaitOne(TimeSpan.FromSeconds(30));

                if (identified)
                    Logger.Info("Found master node.");
            }
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(MasterNodeManager).Namespace);
    }
}
