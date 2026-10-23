#nullable enable

namespace NServiceBus;

/// <summary>
/// Saga timeout reporting configuration extensions.
/// </summary>
public static class SagaTimeoutReportingConfigurationExtensions
{
    extension(EndpointConfiguration endpointConfiguration)
    {
        /// <summary>
        /// Reports saga timeout changes to the ServiceControl error instance and accepts timeout changes requested by an operator.
        /// Requires a saga persister that supports timeout management.
        /// </summary>
        public void ReportSagaTimeoutsToServiceControl()
            => endpointConfiguration.EnableFeature<ReportSagaTimeoutsToPlatform>();
    }
}
