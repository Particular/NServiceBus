namespace NServiceBus
{
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Settings;

    /// <summary>
    /// Allows configuring file-based direct routing table.
    /// </summary>
    public class FileRoutingTableSettings : ExposeSettings
    {
        /// <summary>
        /// Creates new instance.
        /// </summary>
        public FileRoutingTableSettings(SettingsHolder settings) : base(settings)
        {
        }

        /// <summary>
        /// Folder path where to look for files when using file-based direct routing table.
        /// </summary>
        /// <param name="path">The folder path. This can be a UNC path.</param>
        public void LookForFilesIn(string path)
        {
            Settings.Set("FileBasedRouting.BasePath", path);
        }
    }
}