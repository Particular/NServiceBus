namespace NServiceBus;

using System;
using DataBus;

/// <summary>
/// Contains extension methods to <see cref="EndpointConfiguration" /> for the file share data bus.
/// </summary>
public static class ConfigureFileShareDataBus
{
    /// <summary>
    /// Sets the location to which to write/read serialized properties for the databus.
    /// </summary>
    /// <param name="config">The configuration object.</param>
    /// <param name="basePath">The location to which to write/read serialized properties for the databus.</param>
    /// <returns>The configuration.</returns>
    [ObsoleteEx(
        Message = "The DataBus feature has been released as a dedicated package, 'NServiceBus.ClaimCheck.DataBus'",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    public static DataBusExtensions<FileShareDataBus> BasePath(this DataBusExtensions<FileShareDataBus> config, string basePath)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);
        config.Settings.Set("FileShareDataBusPath", basePath);

        return config;
    }
}