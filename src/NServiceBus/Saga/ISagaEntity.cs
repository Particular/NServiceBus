namespace NServiceBus.Saga
{
    /// <summary>
    /// Defines the basic data used by long-running processes.
    /// </summary>
    [ObsoleteEx(Replacement = "IContainSagaData", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
    public interface ISagaEntity:IContainSagaData
    {
       
    }
}
