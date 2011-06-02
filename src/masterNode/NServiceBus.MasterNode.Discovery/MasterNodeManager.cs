using System;
using System.Collections.Generic;
using System.Threading;
using Common.Logging;
using Raven.SituationalAwareness;

namespace NServiceBus.MasterNode.Discovery
{
    public class MasterNodeManager : IManageTheMasterNode
    {
        public Address GetMasterNode()
        {
            if (masterNode != null)
                return masterNode;

            YieldMasterNode(Address.Local, IsCurrentNodeTheMaster, s => masterNode = s);

            Thread.Sleep(presenceInterval + TimeSpan.FromSeconds(1));

            return masterNode;
        }

        public void YieldMasterNode(string localAddress, bool isLocalTheMaster, Action<string> callback)
        {
            var d = new Dictionary<string, string>();
            d["Address"] = localAddress;
            d["Master"] = isLocalTheMaster.ToString();

            var presence = new Presence(Address.Parse(localAddress).Queue, d, presenceInterval);

            presence.TopologyChanged += (sender, nodeMetadata) =>
            {
                switch (nodeMetadata.ChangeType)
                {
                    case TopologyChangeType.MasterSelected:
                        Logger.Debug("Raven.SituationalAwareness selected master: " + nodeMetadata.Metadata["Address"] + " - ignoring.");
                        break;
                    case TopologyChangeType.Discovered:
                        var remoteIsMaster = false;
                        bool.TryParse(nodeMetadata.Metadata["Master"], out remoteIsMaster);

                        if (remoteIsMaster && isLocalTheMaster)
                            throw new InvalidOperationException("This node is configured to be the master yet another master node has been detected: " + nodeMetadata.Metadata["Address"]);

                        var toLog = "Raven.SituationalAwareness identified: " + nodeMetadata.Metadata["Address"];
                        if (remoteIsMaster)
                            toLog += ". Remote node is master.";

                        Logger.Debug(toLog);

                        if (remoteIsMaster)
                            callback(nodeMetadata.Metadata["Address"]);

                        break;
                    case TopologyChangeType.Gone:
                        Logger.Debug("Raven.SituationalAwareness lost communication with: " + nodeMetadata.Metadata["Address"] + " - ignoring.");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            };
            presence.Start();

            if (isLocalTheMaster)
                callback(localAddress);
        }

        public bool IsCurrentNodeTheMaster
        {
            get { return MasterNodeConfigurer.MasterNodeConfigured; }
        }

        public static readonly TimeSpan presenceInterval = TimeSpan.FromSeconds(3);
        private Address masterNode;

        private static readonly ILog Logger = LogManager.GetLogger(typeof (MasterNodeManager).Namespace);
    }
}
