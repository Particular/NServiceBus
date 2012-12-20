namespace NServiceBus
{
    /// <summary>
    /// Indicates that this node will have a Timeout manager
    /// </summary>
    [ObsoleteEx(Message = "Timeout Profile is obsolete as Timeout Manager is on by default for Server and Publisher roles.", RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0")]
    public interface Time : IProfile
    {
    }
}
