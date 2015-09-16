namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.Transports;

    class PipelineModifications
    {
        public List<RegisterStep> Additions = new List<RegisterStep>();
        public List<RemoveStep> Removals = new List<RemoveStep>();
        public List<ReplaceBehavior> Replacements = new List<ReplaceBehavior>();
    }

    class SatellitePipelineModifications : PipelineModifications
    {
        public string Name { get; private set; }

        public string ReceiveAddress { get; private set; }
        
        public ConsistencyGuarantee ConsistencyGuarantee { get; private set; }

        public PushRuntimeSettings RuntimeSettings { get; private set; }


        public SatellitePipelineModifications(string name, string receiveAddress, ConsistencyGuarantee consistencyGuarantee, PushRuntimeSettings runtimeSettings)
        {
            Name = name;
            ReceiveAddress = receiveAddress;
            ConsistencyGuarantee = consistencyGuarantee;
            RuntimeSettings = runtimeSettings;
        }
    }
}