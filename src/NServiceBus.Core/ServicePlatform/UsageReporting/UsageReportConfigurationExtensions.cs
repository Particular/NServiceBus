#nullable enable

namespace NServiceBus;

/// <summary>
/// Usage reporting configuration extensions.
/// </summary>
public static class UsageReportConfigurationExtensions
{
    extension(EndpointConfiguration endpointConfiguration)
    {
        /// <summary>
        /// Periodically report usage information to a ServiceControl instance.
        /// Requires ServiceControl version 6.22.0 or higher.
        /// </summary>
        public void ReportUsageInformationToServiceControl()
            => endpointConfiguration.EnableFeature<SendUsageInfoToPlatform>();
    }
}
