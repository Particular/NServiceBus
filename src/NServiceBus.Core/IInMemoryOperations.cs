namespace NServiceBus
{
    [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Message = "Removed to reduce complexity and API confusion. See http://docs.particular.net/nservicebus/inmemoryremoval for more information.")]
    public interface IInMemoryOperations
    {
    }
}