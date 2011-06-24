using System;
using System.Collections.Generic;
using Common.Logging;
using Raven.SituationalAwareness;

namespace NServiceBus.MasterNode.Discovery
{
    public class MasterNodeManager : IManageTheMasterNode
    {
        public static void Init(string localAddress, bool isLocalTheMaster, Action<string> callback)
        {
            var d = new Dictionary<string, string>();
            d["Address"] = localAddress;
            d["Master"] = isLocalTheMaster.ToString();

            var presence = new PresenceWithoutMasterSelection(Address.Parse(localAddress).Queue, d, presenceInterval);

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
                            masterNode = Address.Parse(nodeMetadata.Metadata["Address"]);

                        if (callback != null && masterNode != null)
                            callback(masterNode);
                            

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
                if (callback != null)
                    callback(localAddress);
        }

        public Address GetMasterNode()
        {
            return masterNode;
        }

        public bool IsCurrentNodeTheMaster { get; set; }
        public Address MasterNode { get; set; }

        public static readonly TimeSpan presenceInterval = TimeSpan.FromSeconds(3);
        private static Address masterNode;

        private static readonly ILog Logger = LogManager.GetLogger(typeof (MasterNodeManager).Namespace);
    }
}
