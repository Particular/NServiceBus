namespace NServiceBus.Features
{
    using NServiceBus.MessagingBestPractices;
    using NServiceBus.Pipeline;

    /// <summary>
    /// Makes sure that messaging best practices are followed
    /// </summary>
    public class BestPracticesEnforcement : Feature
    {
        /// <summary>
        /// Ctor
        /// </summary>
        internal BestPracticesEnforcement()
        {
            EnableByDefault();
        }
        /// <summary>
        /// Initialized the feature
        /// </summary>
        /// <param name="context">The feature context</param>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<Validations>(DependencyLifecycle.InstancePerCall);
            context.Pipeline.Register(WellKnownStep.EnforceBestPractices, typeof(SendValidatorBehavior), "Enforces messaging best practices");
        }

    }
}