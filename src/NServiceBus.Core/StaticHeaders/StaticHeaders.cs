namespace NServiceBus
{
    using Features;

    class StaticHeaders : Feature
    {
        public StaticHeaders()
        {
            EnableByDefault();
            Prerequisite(c => c.Settings.HasSetting<CurrentStaticHeaders>(), "No static outgoing headers registered");
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var headers = context.Settings.Get<CurrentStaticHeaders>();
            context.Pipeline.Register("ApplyStaticHeaders", new ApplyStaticHeadersBehavior(headers), "Applies static headers to outgoing messages");
        }
    }
}