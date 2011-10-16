using System;
using System.Linq;
using System.Net;
using NServiceBus.Config;

namespace NServiceBus.MasterNode.ConfigBacked
{
    public class MasterNodeManager : IManageTheMasterNode
    {
        public Address GetMasterNode()
        {
            if (masterNode == null)
            {
                var section = Configure.GetConfigSection<MasterNodeLocatorConfig>();
                if (section != null)
                    masterNode = section.Node.ToLower();
                else
                    masterNode = Address.Local;
                
            }

            return masterNode;
        }

        public bool IsCurrentNodeTheMaster
        {
            get
            {
                if (GetMasterNode() == null)
                    return false;

                if (GetMasterNode() == Address.Local)
                    return true;
                
                if (Environment.MachineName == masterNode)
                    return true;

                if (Dns.GetHostName() == masterNode)
                    return true;

                IPAddress ip;
                IPAddress.TryParse(masterNode, out ip);
                if (ip != null)
                    if (Dns.GetHostAddresses(Dns.GetHostName()).ToList().Contains(ip))
                        return true;

                return false;
            }
        }

        private Address masterNode; //cached output of GetMasterNode()
    }
}
