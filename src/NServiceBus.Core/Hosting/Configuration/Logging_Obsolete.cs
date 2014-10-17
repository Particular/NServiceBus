﻿#pragma warning disable 1591

namespace NServiceBus
{
    [ObsoleteEx(
        TreatAsErrorFromVersion = "5.0",
        Message = "Use the `NServiceBus.Hosting.Profiles.IConfigureLogging` interface which is contained with in the `NServiceBus.Host` nuget. ",
        RemoveInVersion = "6.0")]
    public interface IConfigureLogging
    {
    }

    [ObsoleteEx(
        TreatAsErrorFromVersion = "5.0", 
        Message = "Use the `NServiceBus.Hosting.Profiles.IConfigureLoggingForProfile` interface which is contained with in the `NServiceBus.Host` nuget. ",
        RemoveInVersion = "6.0")]
    public interface IConfigureLoggingForProfile<T> 
    {
    }

    [ObsoleteEx(
        TreatAsErrorFromVersion = "5.0",
        Message = "Use the `NServiceBus.Hosting.Profiles.IConfigureLogging` interface which is contained with in the `NServiceBus.Host` nuget. ",
        RemoveInVersion = "6.0")]
    public interface IWantCustomLogging
    {
    }
}
