namespace NServiceBus.Distributor
{
    public struct Headers
    {
        public static string WorkerCapacityAvailable = "NServiceBus.Distributor.WorkerCapacityAvailable";
        public static string WorkerStarting = "NServiceBus.Distributor.WorkerStarting";
        public static string ControlMessage = "NServiceBus.Distributor.Control";
    }
}