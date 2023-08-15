namespace NServiceBus
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using ObjectBuilder;
    using Pipeline;

    class PipelineComponent
    {
        PipelineComponent(PipelineModifications modifications)
        {
            this.modifications = modifications;
        }

        public static PipelineComponent Initialize(PipelineSettings settings, HostingComponent.Configuration hostingConfiguration)
        {
            var modifications = settings.modifications;

            foreach (var registeredBehavior in modifications.Replacements)
            {
                hostingConfiguration.Services.AddWithInterfaces(registeredBehavior.BehaviorType, ServiceLifetime.Transient);
            }

            foreach (var step in modifications.Additions)
            {
                step.ApplyContainerRegistration(hostingConfiguration.Services);
            }

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
}