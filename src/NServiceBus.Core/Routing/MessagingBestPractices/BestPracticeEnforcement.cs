namespace NServiceBus.Features
{
    /// <summary>
    /// Makes sure that messaging best practices are followed.
    /// </summary>
    public class BestPracticeEnforcement : Feature
    {
        /// <summary>
        /// Initializes a new instance of <see cref="BestPracticeEnforcement" />.
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
            var validations = new Validations(context.Settings.Get<Conventions>());

            context.Pipeline.Register(
                "EnforceSendBestPractices",
                new EnforceSendBestPracticesBehavior(validations),
                "Enforces send messaging best practices");

            context.Pipeline.Register(
                "EnforceReplyBestPractices",
                new EnforceReplyBestPracticesBehavior(validations),
                "Enforces reply messaging best practices");

            context.Pipeline.Register(
                "EnforcePublishBestPractices",
                new EnforcePublishBestPracticesBehavior(validations),
                "Enforces publish messaging best practices");

            context.Pipeline.Register(
                "EnforceSubscribeBestPractices",
                new EnforceSubscribeBestPracticesBehavior(validations),
                "Enforces subscribe messaging best practices");

            context.Pipeline.Register(
                "EnforceUnsubscribeBestPractices",
                new EnforceUnsubscribeBestPracticesBehavior(validations),
                "Enforces unsubscribe messaging best practices");
        }
    }
}