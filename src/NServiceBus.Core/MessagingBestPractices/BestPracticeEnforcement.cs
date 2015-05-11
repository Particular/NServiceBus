namespace NServiceBus.Features
{
    using NServiceBus.MessagingBestPractices;
    using NServiceBus.Pipeline;

    /// <summary>
    /// Makes sure that messaging best practices are followed
    /// </summary>
    public class BestPracticeEnforcement : Feature
    {
        /// <summary>
        /// Ctor
        /// </summary>
        internal BestPracticeEnforcement()
        {
            EnableByDefault();
        }
        /// <summary>
        /// Initializes the feature
        /// </summary>
        /// <param name="context">The feature context</param>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<Validations>(DependencyLifecycle.SingleInstance);
            context.MainPipeline.Register(WellKnownStep.EnforceBestPractices, typeof(EnforceBestPracticesBehavior), "Enforces messaging best practices");
        }

    }
}