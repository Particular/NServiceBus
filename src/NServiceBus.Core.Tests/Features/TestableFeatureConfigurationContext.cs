namespace NServiceBus.Core.Tests.Features
{
    using NServiceBus.Features;
    using NServiceBus.Pipeline;
    using ObjectBuilder;
    using Settings;

    class TestableFeatureConfigurationContext : FeatureConfigurationContext
    {
        public TestableFeatureConfigurationContext(ReadOnlySettings settings = null, IConfigureComponents container = null, PipelineSettings pipelineSettings = null, RoutingComponent routing = null, EndpointInfo endpointInfo = null) :
            base(settings, container, pipelineSettings, routing, endpointInfo)
        {

        }
    }
}