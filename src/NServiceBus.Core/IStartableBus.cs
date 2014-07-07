namespace NServiceBus
{
    [ObsoleteEx(RemoveInVersion = "6.0",TreatAsErrorFromVersion = "5.0", Replacement = "IBus")]
    public interface IStartableBus : IBus
    {

    }
}