#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Pipeline;

sealed class PipelineComponent
{
    PipelineComponent(PipelineModifications modifications) => this.modifications = modifications;

    public static PipelineComponent Initialize(PipelineSettings settings,
        HostingComponent.Configuration hostingConfiguration, ReceiveComponent.Configuration receiveConfiguration,
        MetersOptions metersOptions)
    {
        // make the PipelineMetrics available to the Pipeline
        hostingConfiguration.Services.AddSingleton(sp =>
        {
            var meterFactory = sp.GetRequiredService<IMeterFactory>();
            // LocalQueueAddress is populated for send-only endpoints too, but that queue is never created, so
            // reporting it as nservicebus.queue would be misleading. Pass null to leave both tags off instead.
            if (receiveConfiguration.IsSendOnlyEndpoint)
            {
                return new PipelineMetrics(meterFactory, null, null, metersOptions);
            }

            string discriminator = receiveConfiguration.InstanceSpecificQueueAddress?.Discriminator ?? "";
            return new PipelineMetrics(meterFactory, receiveConfiguration.LocalQueueAddress.BaseAddress, discriminator, metersOptions);
        });

        return new PipelineComponent(settings.modifications);
    }

    public Pipeline<T> CreatePipeline<T>(IServiceProvider builder) where T : IBehaviorContext => new(builder, modifications);

    public PipelineCache BuildPipelineCache(IServiceProvider rootBuilder) => new(rootBuilder, modifications);

    readonly PipelineModifications modifications;
}