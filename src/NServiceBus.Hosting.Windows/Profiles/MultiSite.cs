namespace NServiceBus
{
    /// <summary>
    /// Indicates that this node has a Gateway access to it.
    /// </summary>
    [ObsoleteEx(
        RemoveInVersion = "6.0", 
        TreatAsErrorFromVersion = "5.0", 
        Replacement = "Feature.Enable<Gateway>")]
    public interface MultiSite : IProfile
    {
    }
}
