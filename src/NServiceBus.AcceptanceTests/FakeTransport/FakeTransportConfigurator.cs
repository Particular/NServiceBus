namespace NServiceBus.AcceptanceTests.FakeTransport
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.Transports;

    public class FakeTransportConfigurator : ConfigureTransport
    {
        public FakeTransportConfigurator()
        {
            Defaults(s => s.SetDefault("FakeTransport.RaiseCriticalErrorDuringStartup", default(Exception)));
        }

        protected override void Configure(FeatureConfigurationContext context, string connectionString)
        {
            context.Container.ConfigureComponent<FakeReceiver>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<FakeQueueCreator>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<FakeDispatcher>(DependencyLifecycle.InstancePerCall);
        }

        protected override bool RequiresConnectionString { get { return false; } }

        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return ""; }
        }
    }
}