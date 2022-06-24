namespace NServiceBus;

using Features;

//TODO review whether a feature provides general value over an internal component/other alternatives
//TODO consider making this public to enable easier opt-out for users
class DiagnosticsFeature : Feature
{
    public DiagnosticsFeature()
    {
        EnableByDefault();
        Prerequisite(_ => ActivitySources.Main.HasListeners(), "Only enable when diagnostic listeners have been registered");
    }

    protected internal override void Setup(FeatureConfigurationContext context)
    {
        context.Pipeline.Register(new SubscribeDiagnosticsBehavior(), "Adds additional subscribe diagnostic attributes to OpenTelemetry spans");
        context.Pipeline.Register(new UnsubscribeDiagnosticsBehavior(), "Adds additional unsubscribe diagnostic attributes to OpenTelemetry spans");
    }
}