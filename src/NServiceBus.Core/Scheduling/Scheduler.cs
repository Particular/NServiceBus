namespace NServiceBus.Features
{
    using System.Collections.Generic;
    using Scheduling;
    using ScheduledTask = Scheduling.Messages.ScheduledTask;

    /// <summary>
    /// NServiceBus scheduling capability you can schedule a task or an action/lambda, to be executed repeatedly in a given interval.
    /// </summary>
    public class Scheduler : Feature
    {
        internal Scheduler()
        {
            Prerequisite(c => !c.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Scheduler cannot be used from a sendonly endpoint");

            EnableByDefault();
        }

        /// <summary>
        /// Invoked if the feature is activated.
        /// </summary>
        /// <param name="context">The feature context.</param>
        protected internal override IReadOnlyCollection<FeatureStartupTask> Setup(FeatureConfigurationContext context)
        {
            context.Settings.Get<Conventions>().AddSystemMessagesConventions(t => typeof(ScheduledTask).IsAssignableFrom(t));
            context.Container.ConfigureComponent<DefaultScheduler>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<ScheduleBehavior>(DependencyLifecycle.SingleInstance);
            context.Pipeline.Register("ScheduleBehavior", typeof(ScheduleBehavior), "Registers a task definition for scheduling.");

            return FeatureStartupTask.None;
        }
    }
}