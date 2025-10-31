namespace NServiceBus;

using Features;

sealed class StaticHeaders : Feature
{
    public StaticHeaders() => Prerequisite(c => c.Settings.HasSetting<CurrentStaticHeaders>(), "No static outgoing headers registered");

    protected override void Setup(FeatureConfigurationContext context)
    {
        var headers = context.Settings.Get<CurrentStaticHeaders>();
        context.Pipeline.Register("ApplyStaticHeaders", new ApplyStaticHeadersBehavior(headers), "Applies static headers to outgoing messages");
    }
}