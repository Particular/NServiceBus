#nullable enable

namespace NServiceBus;

using System;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Features;

class SendUsageInfoToPlatform : Feature
{
    public SendUsageInfoToPlatform()
    {
        DependsOn<ServicePlatformFeature>();
        Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"),
            "Usage Information is only relevant for endpoints receiving messages.");
        Defaults(settings => settings.SetDefault(ReportingIntervalSettingKey, TimeSpan.FromDays(1)));
    }

    protected override void Setup(FeatureConfigurationContext context)
        => context.RegisterStartupTask(
            serviceProvider =>
            {
                var servicePlatformSender = serviceProvider.GetRequiredService<ServicePlatform>();
                var endpointUsageReportSender = servicePlatformSender.CreatePrimarySender(UsageReportingMessagesJsonContext.Default.EndpointUsageReport);

                var settings = new UsageReporterSettings
                {
                    EndpointName = context.Settings.EndpointName(),
                    ReportingInterval = context.Settings.Get<TimeSpan>(ReportingIntervalSettingKey)
                };

                return new UsageReporter(endpointUsageReportSender, settings);
            }
        );

    public const string ReportingIntervalSettingKey = "UsageReporting.ReportingInterval";
}
