namespace NServiceBus.MasterNode
{
    using Config;

    class DefaultMasterNodeManager
    {
        public static string DetermineMasterNode()
        {
            var section = Configure.GetConfigSection<MasterNodeConfig>();
            if (section != null)
                return section.Node;
            
            return "localhost";
        }
    }
}