namespace NServiceBus
{
    /// <summary>
    /// Indicates this endpoint is a publisher.
    /// This is compatible with <see cref="AsA_Server"/> but not <see cref="AsA_Client"/>.
    /// </summary>
    public interface AsA_Publisher : AsA_Server {}
}
