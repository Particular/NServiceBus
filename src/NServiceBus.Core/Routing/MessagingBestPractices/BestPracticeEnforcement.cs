namespace NServiceBus.Features
{
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
        protected internal override void Setup(FeatureConfigurationContext context)
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
        }

    }
}