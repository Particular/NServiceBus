namespace NServiceBus
{
    class ReceiveConfiguration
    {
        public ReceiveConfiguration(LogicalAddress logicalAddress, string mainQueueName, string localAddress, string instanceSpecificQueue, bool isEnabled)
        {
            LogicalAddress = logicalAddress;
            MainQueueName = mainQueueName;
            LocalAddress = localAddress;
            InstanceSpecificQueue = instanceSpecificQueue;
            IsEnabled = isEnabled;
        }

        public LogicalAddress LogicalAddress { get; }

        public string LocalAddress { get; }

        public string InstanceSpecificQueue { get; }

        public string MainQueueName { get; }

        public bool IsEnabled { get; }
    }
}