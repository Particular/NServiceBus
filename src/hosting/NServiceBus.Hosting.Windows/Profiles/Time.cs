namespace NServiceBus
{
    /// <summary>
    /// Indicates that this node will have a Timeout manager
    /// </summary>
    [ObsoleteEx(Message = "Timeout Profile is obsolete as Timeout Manager is on by default for Server and Publisher roles.", RemoveInVersion = "4.0", TreatAsErrorFromVersion = "3.4")]
    public interface Time : IProfile
    {
    }
}
