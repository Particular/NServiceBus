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
    public class Bootstrapper : INeedInitialization
    {
        public void Init()
        {
            if (!RoutingConfig.IsDynamicNodeDiscoveryOn)
                return;

            var cfg = Configure.Instance.Configurer.ConfigureComponent<MasterNodeManager>(DependencyLifecycle.SingleInstance);

            var localQueue = GenerateLocalQueueFromMessageTypes(GetMessageTypesThisEndpointOwns());

            if (RoutingConfig.IsConfiguredAsMasterNode)
            {
                //StartMasterPresence(localQueue, localQueue);
                cfg.ConfigureProperty(m => m.IsCurrentNodeTheMaster, true);
                cfg.ConfigureProperty(m => m.MasterNode, localQueue);
            }
            else
            {
                
            }

            Configure.ConfigurationComplete += (o, e) => configIsComplete = true;

            GetMessageTypesThisEndpointOwns().ToList().ForEach(t =>
                StartMasterPresence(localQueue, t.FullName));

            GetAllMessageTypes().Except(GetMessageTypesThisEndpointOwns()).ToList().ForEach(t =>
                DetectMasterPresence(t.FullName, a =>
                                                     {
                                                         var action = new EventHandler((o, e) =>
                                                                                {
                                                                                    var bus =
                                                                                        Configure.Instance.Builder.Build<UnicastBus>();
                                                                                    bus.RegisterMessageType(t, a, false);
                                                                                });
                                                         if (configIsComplete)
                                                             action(null, null);
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
                                if (masterDetected != null)
                                    masterDetected(Address.Parse(nodeMetadata.Metadata["Address"]));

                        break;
                }
            };

            presence.Start();
        }

        private static void StartMasterPresence(string localQueue, string topic)
        {
            var d = new Dictionary<string, string>();
            d["Address"] = localQueue;

            var presence = new PresenceWithoutMasterSelection(topic, d, presenceInterval);
            
            presence.Start();
        }

        private static Address GenerateLocalQueueFromMessageTypes(IEnumerable<Type> messageTypes)
        {
            if (messageTypes == null || messageTypes.Count() == 0)
                return Address.Local;

            var queue = string.Join("_", messageTypes.Select(t => t.FullName));
            return Address.Parse(queue);
        }

        private static IEnumerable<Type> GetAllMessageTypes()
        {
            return Configure.TypesToScan.Where(
                t => typeof (IMessage).IsAssignableFrom(t) && t != typeof(IMessage) && !t.Namespace.Contains("NServiceBus"));
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

        private static TimeSpan presenceInterval = TimeSpan.FromSeconds(0.5);
        private static bool configIsComplete;
    }
}
