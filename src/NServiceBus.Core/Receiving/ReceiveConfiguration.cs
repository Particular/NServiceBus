namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Transport;

    class ReceiveConfiguration
    {
        public ReceiveConfiguration(LogicalAddress logicalAddress, string mainQueueName, string localAddress, string instanceSpecificQueue, TransportTransactionMode transactionMode, bool isEnabled)
        {
            LogicalAddress = logicalAddress;
            MainQueueName = mainQueueName;
            LocalAddress = localAddress;
            InstanceSpecificQueue = instanceSpecificQueue;
            TransactionMode = transactionMode;
            IsEnabled = isEnabled;

            SatelliteDefinitions = new SatelliteDefinitions();
        }

        public LogicalAddress LogicalAddress { get; }

        public string LocalAddress { get; }

        public string InstanceSpecificQueue { get; }
        public TransportTransactionMode TransactionMode { get; }

        public string MainQueueName { get; }

        public bool IsEnabled { get; }

        public void AddSatelliteReceiver(string name, string transportAddress, PushRuntimeSettings runtimeSettings, Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy, Func<IBuilder, MessageContext, Task> onMessage)
        {
            var satelliteDefinition = new SatelliteDefinition(name, transportAddress, TransactionMode, runtimeSettings, recoverabilityPolicy, onMessage);

            SatelliteDefinitions.Add(satelliteDefinition);
        }

        public SatelliteDefinitions SatelliteDefinitions{ get; }

    }
}