using System;
using System.Collections.Generic;
using System.Linq;
using Common.Logging;
using NServiceBus.Config;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast;
using Raven.SituationalAwareness;

namespace NServiceBus.MasterNode.Discovery
{
    public class Bootstrapper : INeedInitialization, IWantToRunWhenConfigurationIsComplete
    {
        public static IEnumerable<Type> MessageTypesOwned { get; private set; }

        public void Init()
        {
            if (!RoutingConfig.IsDynamicNodeDiscoveryOn)
                return;

            Configure.Instance.Configurer.ConfigureComponent<OwnershipChecker>(DependencyLifecycle.InstancePerCall);

            var cfg = Configure.Instance.Configurer.ConfigureComponent<MasterNodeManager>(DependencyLifecycle.SingleInstance);

            if (RoutingConfig.IsConfiguredAsMasterNode)
            {
                StartMasterPresence(Address.Local);
                cfg.ConfigureProperty(m => m.IsCurrentNodeTheMaster, true);
            }
            else
            {
                DetectMasterPresence(Address.Local, a => cfg.ConfigureProperty(m => m.MasterNode, a));
            }

            Configure.ConfigurationComplete += () => configIsComplete = true;
        }

        public void Run()
        {
            MessageTypesOwned = GetMessageTypesThisEndpointOwns();
            MessageTypesOwned.ToList().ForEach(t =>
                StartMasterPresence(t.FullName));

            GetAllMessageTypes().Except(GetMessageTypesThisEndpointOwns()).ToList().ForEach(t =>
                DetectMasterPresence(t.FullName, a =>
                {
                    Action action = () =>
                    {
                        var bus =
                            Configure.Instance.Builder.Build<UnicastBus>();
                        bus.RegisterMessageType(t, a, false);
                    };
                    if (configIsComplete)
                        action();
                    else
                        Configure.ConfigurationComplete += action;
                })
                );
        }

        private static void DetectMasterPresence(string topic, Action<Address> masterDetected)
        {
            var presence = new PresenceWithoutMasterSelection(topic, new Dictionary<string, string>(), presenceInterval);

            presence.TopologyChanged += (sender, nodeMetadata) =>
            {
                switch (nodeMetadata.ChangeType)
                {
                    case TopologyChangeType.Discovered:

                        if (nodeMetadata.Metadata.ContainsKey("Address"))
                            if (nodeMetadata.Metadata["Address"] != null)
                            {
                                Logger.Info("Heard from broadcaster: " + nodeMetadata.Metadata["Address"] + " about topic: " + topic);
                                if (masterDetected != null)
                                    masterDetected(Address.Parse(nodeMetadata.Metadata["Address"]));
                            }
                        break;
                }
            };

            presence.Start();

            Logger.Info("Listening for broadcasts about topic: " + topic);
        }

        private static void StartMasterPresence(string topic)
        {
            new PresenceWithoutMasterSelection(
                topic, 
                new Dictionary<string, string>
                    {
                        {"Address", Address.Local }
                    }, 
                presenceInterval)
            .Start();

            Logger.Info("Broadcasting ownership of topic: " + topic);
        }

        private static IEnumerable<Type> GetAllMessageTypes()
        {
            return Configure.TypesToScan.Where(
                t => typeof (IMessage).IsAssignableFrom(t) && t != typeof(IMessage) && !t.Namespace.StartsWith("NServiceBus"));
        }

        private static IEnumerable<Type> GetMessageTypesThisEndpointOwns()
        {
            foreach(Type t in Configure.TypesToScan)
                foreach(Type i in t.GetInterfaces())
                        {
                            var args = i.GetGenericArguments();
                            if (args.Length == 1)
                                if (typeof(IMessage).IsAssignableFrom(args[0]))
                                    if (i == typeof(IAmResponsibleForMessages<>).MakeGenericType(args))
                                        yield return args[0];
                        }
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(MasterNodeManager).Namespace);

        private static TimeSpan presenceInterval = TimeSpan.FromSeconds(10);
        private static bool configIsComplete;
    }
}
