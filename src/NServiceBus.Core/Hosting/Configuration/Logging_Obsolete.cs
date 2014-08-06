#pragma warning disable 1591

namespace NServiceBus
{
    [ObsoleteEx(TreatAsErrorFromVersion = "5.0", Replacement = "NServiceBus.Hosting.Profiles.IConfigureLogging", RemoveInVersion = "6.0")]
    public interface IConfigureLogging
    {
    }

    [ObsoleteEx(TreatAsErrorFromVersion = "5.0", Replacement = "NServiceBus.Hosting.Profiles.IConfigureLoggingForProfile", RemoveInVersion = "6.0")]
    public interface IConfigureLoggingForProfile<T> 
    {
    }

    [ObsoleteEx(TreatAsErrorFromVersion = "5.0", Replacement = "NServiceBus.Hosting.Profiles.IConfigureLogging", RemoveInVersion = "6.0")]
    public interface IWantCustomLogging
    {
    }
}
