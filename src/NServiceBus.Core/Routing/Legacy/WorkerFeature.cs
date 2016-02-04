namespace NServiceBus
{
    using System;
    using Features;
    using Transports;

    class WorkerFeature : Feature
    {
        internal WorkerFeature()
        {
            Defaults(s =>
            {
                if (s.GetOrDefault<string>("PublicReturnAddress") != null)
                {
                    throw new Exception("We detected you have overridden the public return address with EndpointConfiguration. In order to enlist with a legacy distributor you need to remove this override as the public address needs to be set to the distributor address.");
                }
                s.Set("PublicReturnAddress", s.Get<string>("LegacyDistributor.Address"));
            });
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var masterNodeControlAddress = context.Settings.Get<string>("LegacyDistributor.ControlAddress");
            var capacity = context.Settings.Get<int>("LegacyDistributor.Capacity");

            context.Container.ConfigureComponent(b => new ProcessedMessageCounterBehavior(b.Build<ReadyMessageSender>()), DependencyLifecycle.SingleInstance);

            context.RegisterStartupTask(b => new ReadyMessageSender(b.Build<IDispatchMessages>(), context.Settings.LocalAddress(), capacity, masterNodeControlAddress));
            context.Pipeline.Register("ProcessedMessageCounterBehavior", typeof(ProcessedMessageCounterBehavior), "Counts messages processed by the worker.");
        }
    }
}
