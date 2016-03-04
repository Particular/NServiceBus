namespace NServiceBus
{
    using Transports;

    class SatellitePipelineModifications : PipelineModifications
    {
        public SatellitePipelineModifications(string name, string receiveAddress, TransportTransactionMode requiredTransportTransactionMode, PushRuntimeSettings runtimeSettings)
        {
            Name = name;
            ReceiveAddress = receiveAddress;
            RequiredTransportTransactionMode = requiredTransportTransactionMode;
            RuntimeSettings = runtimeSettings;
        }

        public string Name { get; private set; }

        public string ReceiveAddress { get; private set; }

        public TransportTransactionMode RequiredTransportTransactionMode { get; private set; }

        public PushRuntimeSettings RuntimeSettings { get; private set; }
    }
}