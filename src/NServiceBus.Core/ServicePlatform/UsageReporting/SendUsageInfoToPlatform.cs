#nullable enable

namespace NServiceBus;

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NServiceBus.Features;
using NServiceBus.ServicePlatform;

class SendUsageInfoToPlatform : Feature
{
    public SendUsageInfoToPlatform()
    {
        DependsOn<ServicePlatformFeature>();
        Defaults(settings => settings.SetDefault(ReportingIntervalSettingKey, TimeSpan.FromMinutes(10)));
    }

    protected override void Setup(FeatureConfigurationContext context)
        => context.RegisterStartupTask(
            serviceProvider =>
            {
                var servicePlatform = serviceProvider.GetRequiredService<ServicePlatformConnection>();

                var settings = new UsageReporterSettings
                {
                    EndpointName = context.Settings.EndpointName(),
                    BaseQueueAddress = context.Receiving.LocalQueueAddress.BaseAddress,
                    ReportingInterval = context.Settings.Get<TimeSpan>(ReportingIntervalSettingKey)
                };

                var logger = serviceProvider.GetRequiredService<ILogger<UsageReporter>>();

                return new UsageReporter(servicePlatform.PrimaryInstance, settings, TimeProvider.System, logger);
            }
        );

    public const string ReportingIntervalSettingKey = "UsageReporting.ReportingInterval";
}
