namespace NServiceBus.Distributor.ReadyMessages
{
    [ObsoleteEx(Message = "Not a public API.", TreatAsErrorFromVersion = "4.3", RemoveInVersion = "5.0")]    
    public struct Headers
    {
        public static string WorkerCapacityAvailable = "NServiceBus.Distributor.WorkerCapacityAvailable";
        public static string WorkerStarting = "NServiceBus.Distributor.WorkerStarting";
    }
}