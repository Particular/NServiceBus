namespace NServiceBus
{
    /// <summary>
    /// Used to enable InMemory persistence.
    /// </summary>
    [ObsoleteEx(Message = "The InMemoryPersistence has been moved to a dedicated Nuget Package called NServiceBus.Persistence.NonDurable and has been renamed to NonDurablePersistence", TreatAsErrorFromVersion = "8.0.0", RemoveInVersion = "9.0.0")]
    public class InMemoryPersistence
    {
    }
}