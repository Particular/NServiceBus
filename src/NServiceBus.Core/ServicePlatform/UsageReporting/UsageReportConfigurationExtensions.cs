#nullable enable

namespace NServiceBus;

using NServiceBus.Features;

/// <summary>
/// Usage reporting configuration extensions.
/// </summary>
public static class UsageReportConfigurationExtensions
{
    extension(ServicePlatformSettings settings)
    {
        /// <summary>
        /// Enable sending usage information to the service platform.
        /// </summary>
        public void SendUsageInformation()
        {
            settings.Settings.EnableFeature<SendUsageInfoToPlatform>();
        }
    }
}
