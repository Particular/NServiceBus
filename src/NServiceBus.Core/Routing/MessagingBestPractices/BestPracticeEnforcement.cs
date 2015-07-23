namespace NServiceBus.Features
{
    using NServiceBus.MessagingBestPractices;

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

            context.Pipeline.Register("EnforceSendBestPractices", typeof(EnforceSendBestPracticesBehavior), "Enforces send messaging best practices");
            context.Pipeline.Register("EnforceReplyBestPractices", typeof(EnforceReplyBestPracticesBehavior), "Enforces reply messaging best practices");
            context.Pipeline.Register("EnforcePublishBestPractices", typeof(EnforcePublishBestPracticesBehavior), "Enforces publish messaging best practices");
        }

    }
}