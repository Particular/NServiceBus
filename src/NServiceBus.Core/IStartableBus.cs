namespace NServiceBus
{ 
    /// <summary>
    /// The interface used for starting and stopping an IBus.
    /// </summary>
    [ObsoleteEx(RemoveInVersion = "6.0",TreatAsErrorFromVersion = "5.0", Replacement = "IBus")]
    public interface IStartableBus : IBus
    {

    }
}