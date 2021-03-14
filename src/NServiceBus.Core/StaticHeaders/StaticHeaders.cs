namespace NServiceBus
{
    using System.Threading.Tasks;
    using System.Threading;
    using Features;

    class StaticHeaders : Feature
    {
        public StaticHeaders()
        {
            EnableByDefault();
            Prerequisite(c => c.Settings.HasSetting<CurrentStaticHeaders>(), "No static outgoing headers registered");
        }

        protected internal override Task Setup(FeatureConfigurationContext context, CancellationToken cancellationToken = default)
        {
            var headers = context.Settings.Get<CurrentStaticHeaders>();
            context.Pipeline.Register("ApplyStaticHeaders", new ApplyStaticHeadersBehavior(headers), "Applies static headers to outgoing messages");

            return Task.CompletedTask;
        }
    }
}