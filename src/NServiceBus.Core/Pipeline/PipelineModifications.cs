namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using NServiceBus.Transports;

    class PipelineModifications
    {
        public List<RegisterStep> Additions = new List<RegisterStep>();
        public List<RemoveStep> Removals = new List<RemoveStep>();
        public List<ReplaceBehavior> Replacements = new List<ReplaceBehavior>();
    }

    class SatellitePipelineModifications : PipelineModifications
    {
        public SatellitePipelineModifications(string name, string receiveAddress, TransactionSupport requiredTransactionSupport, PushRuntimeSettings runtimeSettings)
        {
            Name = name;
            ReceiveAddress = receiveAddress;
            RequiredTransactionSupport = requiredTransactionSupport;
            RuntimeSettings = runtimeSettings;
        }

        public string Name { get; private set; }

        public string ReceiveAddress { get; private set; }

        public TransactionSupport RequiredTransactionSupport { get; private set; }

        public PushRuntimeSettings RuntimeSettings { get; private set; }
    }
}