namespace NServiceBus
{
    using Features;

    class WorkerFeature : Feature
    {
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var masterNodeControlAddress = context.Settings.Get<string>("LegacyDistributor.ControlAddress");
            var capacity = context.Settings.Get<int>("LegacyDistributor.Capacity");

            context.Container.ConfigureComponent(() => new ReadyMessageSender(context.Transport.Dispatcher, context.Transport.SharedQueue, capacity, masterNodeControlAddress), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => new ProcessedMessageCounterBehavior(b.Build<ReadyMessageSender>()), DependencyLifecycle.SingleInstance);

            context.RegisterStartupTask(b => b.Build<ReadyMessageSender>());
            context.Pipeline.Register("ProcessedMessageCounterBehavior", b => b.Build<ProcessedMessageCounterBehavior>(), "Counts messages processed by the worker.");
        }
    }
}