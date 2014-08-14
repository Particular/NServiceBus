namespace NServiceBus
{
    /// <summary>
    /// Indicates this endpoint is a publisher.
    /// This is compatible with <see cref="AsA_Server"/> but not <see cref="AsA_Client"/>.
    /// </summary>
    [ObsoleteEx(RemoveInVersion = "6.0", Replacement = "AsA_Server", Message = "This is no longer required and can be safely replaced with AsA_Server.")]
    public interface AsA_Publisher  {}
}
