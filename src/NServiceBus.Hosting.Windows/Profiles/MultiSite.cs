namespace NServiceBus
{
    /// <summary>
    /// Indicates that this node has a Gateway access to it.
    /// </summary>
    [ObsoleteEx(
        RemoveInVersion = "6.0", 
        TreatAsErrorFromVersion = "5.0",
        Replacement = "MultiSite Profile is now obsolete. Gateway has been moved to its own stand alone nuget 'NServiceBus.Gateway'. To enable Gateway, install the nuget package and then call `configuration.EnableFeature<Gateway>()`, where `configuration` is an instance of type `BusConfiguration`.")]
    public interface MultiSite : IProfile
    {
    }
}
