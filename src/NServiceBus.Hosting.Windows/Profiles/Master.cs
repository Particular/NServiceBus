namespace NServiceBus
{
    /// <summary>
    /// Indicates that this node is the master node for its set of messages.
    /// </summary>
    [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0", Message = "The NServiceBus Distributor was moved into its own assembly (NServiceBus.Distributor.MSMQ.dll), please make sure you reference the new assembly.")]
    public interface Master : IProfile
    {
    }
}
