namespace NServiceBus;

using System;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Pipeline;

sealed class PipelineComponent
{
    PipelineComponent(PipelineModifications modifications) => this.modifications = modifications;

    public static PipelineComponent Initialize(PipelineSettings settings,
        HostingComponent.Configuration hostingConfiguration, ReceiveComponent.Configuration receiveConfiguration)
    {
        // make the PipelineMetrics available to the Pipeline
        hostingConfiguration.Services.AddSingleton(sp =>
        {
            var meterFactory = sp.GetService<IMeterFactory>();
            string discriminator = receiveConfiguration.InstanceSpecificQueueAddress?.Discriminator ?? "";
            return new IncomingPipelineMetrics(meterFactory, receiveConfiguration.LocalQueueAddress.BaseAddress, discriminator);
        });

        return new PipelineComponent(settings.modifications);
    }

    public Pipeline<T> CreatePipeline<T>(IServiceProvider builder) where T : IBehaviorContext => new(builder, modifications);

    public PipelineCache BuildPipelineCache(IServiceProvider rootBuilder) => new(rootBuilder, modifications);

    readonly PipelineModifications modifications;
}