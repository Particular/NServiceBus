namespace NServiceBus.Features
{
    class BestPracticesFeature : Feature
    {
        public const string EnforceBestPracticesSettingsKey = "NServiceBus.Routing.EnforceBestPractices";

        public BestPracticesFeature()
        {
            EnableByDefault();
            Defaults(s =>
            {
                s.SetDefault(EnforceBestPracticesSettingsKey, true);
            });
            Prerequisite(c => c.Settings.Get<bool>(EnforceBestPracticesSettingsKey), "Best practices enforcement is disabled.");
        }

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