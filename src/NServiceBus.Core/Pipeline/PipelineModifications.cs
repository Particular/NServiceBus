namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    class PipelineModifications
    {
        public List<RegisterStep> Additions = new List<RegisterStep>();
        public List<RemoveStep> Removals = new List<RemoveStep>();
        public List<ReplaceBehavior> Replacements = new List<ReplaceBehavior>();
    }

    class SatellitePipelineModifications : PipelineModifications
    {
        public string Name { get; private set; }
    
        public string ReceiveAddress{ get; private set; }
       
        public TransactionSettings TransactionSettings { get; private set; }

        public PushRuntimeSettings PushRuntimeSettings{ get; private set; }


        public SatellitePipelineModifications(string name, string receiveAddress,TransactionSettings transactionSettings = null, PushRuntimeSettings pushRuntimeSettings = null)
        {
            Name = name;
            ReceiveAddress = receiveAddress;
            TransactionSettings = transactionSettings;
            PushRuntimeSettings = pushRuntimeSettings;
        }
    }
}