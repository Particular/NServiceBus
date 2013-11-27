namespace NServiceBus.Distributor.MSMQ.ReadyMessages
{
    internal static class Headers
    {
        public const string WorkerCapacityAvailable = "NServiceBus.Distributor.WorkerCapacityAvailable";
        public const string WorkerStarting = "NServiceBus.Distributor.WorkerStarting";
        public const string UnregisterWorker = "NServiceBus.Distributor.UnregisterWorker";
        public const string WorkerSessionId = "NServiceBus.Distributor.WorkerSessionId";
    }
}