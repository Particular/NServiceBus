namespace NServiceBus.Features
{
    using System.Collections.Generic;
    using NServiceBus.MessagingBestPractices;
    using NServiceBus.Pipeline;

    /// <summary>
    /// Makes sure that messaging best practices are followed.
    /// </summary>
    public class BestPracticeEnforcement : Feature
    {
        /// <summary>
        /// Initializes a new instance of <see cref="BestPracticeEnforcement"/>.
        /// </summary>
        internal BestPracticeEnforcement()
        {
            EnableByDefault();
        }

        /// <summary>
        /// Initializes the feature.
        /// </summary>
        /// <param name="context">The feature context.</param>
        protected internal override IReadOnlyCollection<FeatureStartupTask> Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<Validations>(DependencyLifecycle.SingleInstance);

            context.Pipeline.Register(
                WellKnownStep.EnforceSendBestPractices,
                typeof(EnforceSendBestPracticesBehavior),
                "Enforces send messaging best practices");

            context.Pipeline.Register(
                WellKnownStep.EnforceReplyBestPractices,
                typeof(EnforceReplyBestPracticesBehavior),
                "Enforces reply messaging best practices");

            context.Pipeline.Register(
                WellKnownStep.EnforcePublishBestPractices,
                typeof(EnforcePublishBestPracticesBehavior),
                "Enforces publish messaging best practices");

            context.Pipeline.Register(
                WellKnownStep.EnforceSubscribeBestPractices,
                typeof(EnforceSubscribeBestPracticesBehavior),
                "Enforces subscribe messaging best practices");

            context.Pipeline.Register(
                WellKnownStep.EnforceUnsubscribeBestPractices,
                typeof(EnforceUnsubscribeBestPracticesBehavior),
                "Enforces unsubscribe messaging best practices");

            return FeatureStartupTask.None;
        }

    }
}