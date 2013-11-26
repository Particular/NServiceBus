namespace NServiceBus
{
    /// <summary>
    /// Feature Profile for starting the Distributor without a worker running on its endpoint
    /// </summary>
    [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0", Message = "The NServiceBus Distributor was moved into its own assembly (NServiceBus.Distributor.MSMQ.dll), please make sure you reference the new assembly.")]
    public interface Distributor : IProfile
    {
    }
}
