namespace NServiceBus
{
    using Features;
    using Transport;

    class WorkerFeature : Feature
    {
        public WorkerFeature()
        {
            DependsOn<DelayedDeliveryFeature>();
            Defaults(s =>
            {
                var distributorAddress = s.Get<string>("LegacyDistributor.Address");
                var distributorMsmqAddress = MsmqAddress.Parse(distributorAddress);
                var distributorTimeoutQueue = new MsmqAddress(distributorMsmqAddress.Queue + ".Timeouts", distributorMsmqAddress.Machine);
                var timeoutManagerAddressConfiguration = s.Get<TimeoutManagerAddressConfiguration>();
                timeoutManagerAddressConfiguration.Set(distributorTimeoutQueue.ToString());
            });
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var distributorControlAddress = context.Settings.Get<string>("LegacyDistributor.ControlAddress");
            var capacity = context.Settings.Get<int>("LegacyDistributor.Capacity");

            context.Container.ConfigureComponent(b => new ReadyMessageSender(b.Build<IDispatchMessages>(), context.Settings.LocalAddress(), capacity, distributorControlAddress), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => new ProcessedMessageCounterBehavior(b.Build<ReadyMessageSender>(), context.Settings.Get<NotificationSubscriptions>()), DependencyLifecycle.SingleInstance);

            context.RegisterStartupTask(b => b.Build<ReadyMessageSender>());
            context.Pipeline.Register("ProcessedMessageCounterBehavior", b => b.Build<ProcessedMessageCounterBehavior>(), "Counts messages processed by the worker.");
        }
    }
}