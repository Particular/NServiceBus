namespace NServiceBus;

using Microsoft.Extensions.DependencyInjection;
using Features;

class Tracing : Feature
{
    public Tracing()
    {
        EnableByDefault(); // TODO should be explicitly enabled
        Prerequisite(c => ActivitySources.Main.HasListeners(), "No trace listeners have been registered");
    }

    protected internal override void Setup(FeatureConfigurationContext context)
    {
        context.Services.AddSingleton(new ActivityFactory()); //TODO: Should be done in HostingComponent instead of a feature
    }
}