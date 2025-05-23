namespace NServiceBus;

using System;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Pipeline;

class PipelineComponent
{
    PipelineComponent(PipelineModifications modifications)
    {
        this.modifications = modifications;
    }

    public static PipelineComponent Initialize(PipelineSettings settings,
        HostingComponent.Configuration hostingConfiguration, ReceiveComponent.Configuration receiveConfiguration)
    {
        var modifications = settings.modifications;

        foreach (var registeredBehavior in modifications.Replacements)
        {
            hostingConfiguration.Services.AddTransient(registeredBehavior.BehaviorType);
        }

        foreach (var step in modifications.Additions)
        {
            step.ApplyContainerRegistration(hostingConfiguration.Services);
        }

        // make the PipelineMetrics available to the Pipeline 
        hostingConfiguration.Services.AddSingleton(sp =>
        {
            var meterFactory = sp.GetService<IMeterFactory>();
            string discriminator = receiveConfiguration.InstanceSpecificQueueAddress?.Discriminator ?? "";
            return new IncomingPipelineMetrics(meterFactory, receiveConfiguration.LocalQueueAddress.BaseAddress, discriminator);
        });

        return new PipelineComponent(modifications);
    }

    public Pipeline<T> CreatePipeline<T>(IServiceProvider builder) where T : IBehaviorContext
    {
        return new Pipeline<T>(builder, modifications);
    }

    public PipelineCache BuildPipelineCache(IServiceProvider rootBuilder)
    {
        return new PipelineCache(rootBuilder, modifications);
    }

    readonly PipelineModifications modifications;
}