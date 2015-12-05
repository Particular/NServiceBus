namespace NServiceBus
{
    using NServiceBus.Transports;

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