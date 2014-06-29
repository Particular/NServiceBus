namespace NServiceBus
{
    using Features;
    using Scheduling;
    using ScheduledTask = Scheduling.Messages.ScheduledTask;

    /// <summary>
    /// NServiceBus scheduling capability you can schedule a task or an action/lambda, to be executed repeatedly in a given interval.
    /// </summary>
    public class Scheduler : Feature
    {
        internal Scheduler()
        {
            DependsOn<TimeoutManagerBasedDeferral>();

            EnableByDefault();
        }
        /// <summary>
        /// Invoked if the feature is activated
        /// </summary>
        /// <param name="context">The feature context</param>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Settings.Get<Conventions>().AddSystemMessagesConventions(t => typeof(ScheduledTask).IsAssignableFrom(t));
            context.Container.ConfigureComponent<DefaultScheduler>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<Schedule>(DependencyLifecycle.SingleInstance);
        }
    }
}