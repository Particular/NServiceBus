namespace NServiceBus.Outbox
{
    /// <summary>
    /// Implemented by the persisters to provide outbox storage capabilities
    /// </summary>
    [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", 
        Message = "IOutboxStorage has been split into IStoreOutboxMessages and IDeduplicateMessages to segregate storage concerns.")]
    public interface IOutboxStorage : IStoreOutboxMessages, IDeduplicateMessages
    {
    }
}