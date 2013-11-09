namespace NServiceBus.Config
{
    using System.Configuration;

    /// <summary>
    /// Configuration section for holding the node which is the master.
    /// </summary>
    [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0", Message = "The NServiceBus Distributor was moved into its own assembly (NServiceBus.Distributor.MSMQ.dll), please make sure you reference the new assembly.")]
    public class MasterNodeConfig : ConfigurationSection
    {
        /// <summary>
        /// The node .
        /// </summary>
        [ConfigurationProperty("Node", IsRequired = true)]
        public string Node
        {
            get
            {
                return this["Node"] as string;
            }
            set
            {
                this["Node"] = value;
            }
        }
    }
}
