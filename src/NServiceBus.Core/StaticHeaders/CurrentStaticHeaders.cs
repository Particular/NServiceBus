namespace NServiceBus.StaticHeaders
{
    using System.Collections.Generic;
    using NServiceBus.Features;

    class StaticHeaders:Feature
    {
        public StaticHeaders()
        {
            EnableByDefault();
            Prerequisite(c=>c.Settings.HasSetting<CurrentStaticHeaders>(),"No static outgoing headers registered");

        }

        protected internal override IReadOnlyCollection<FeatureStartupTask> Setup(FeatureConfigurationContext context)
        {
            var headers = context.Settings.Get<CurrentStaticHeaders>();

            context.Container.ConfigureComponent(b => new ApplyStaticHeadersBehavior(headers), DependencyLifecycle.SingleInstance);
            context.Pipeline.Register("ApplyStaticHeaders", typeof(ApplyStaticHeadersBehavior), "Applies static headers to outgoing messages");
            return FeatureStartupTask.None;
        }
    }

    class CurrentStaticHeaders:Dictionary<string,string>
    {
    }
}