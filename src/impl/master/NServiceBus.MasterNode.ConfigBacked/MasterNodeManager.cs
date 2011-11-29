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
                    masterNode =  Address.Parse(section.Node.ToLower());
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

                if (Address.Parse(Environment.MachineName) == GetMasterNode())
                    return true;

                if (Address.Parse(Dns.GetHostName()) == GetMasterNode())
                    return true;

                IPAddress ip;
                IPAddress.TryParse(GetMasterNode().ToString(), out ip);
                if (ip != null)
                    if (Dns.GetHostAddresses(Dns.GetHostName()).ToList().Contains(ip))
                        return true;

                return false;
            }
        }

        private Address masterNode; //cached output of GetMasterNode()
    }
}
