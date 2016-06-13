namespace NServiceBus.Features
{
    using Config;
    using ConsistencyGuarantees;
    using Settings;

    /// <summary>
    /// Used to configure Second Level Retries.
    /// </summary>
    //todo: obsolete
    public class FirstLevelRetries : Feature
    {
        internal FirstLevelRetries()
        {
            EnableByDefault();

            DependsOn<StoreFaultsInErrorQueue>();

            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Send only endpoints can't use FLR since it only applies to messages being received");

            Prerequisite(context => context.Settings.GetRequiredTransactionModeForReceives() != TransportTransactionMode.None, "Transactions must be enabled since FLR requires the transport to be able to rollback");

            Prerequisite(context => GetMaxRetries(context.Settings) > 0, "FLR was disabled in config since it's set to 0");
        }

        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var transportConfig = context.Settings.GetConfigSection<TransportConfig>();
            var maxRetries = transportConfig?.MaxRetries ?? 5;
            var retryPolicy = new FirstLevelRetryPolicy(maxRetries);
            context.Container.RegisterSingleton(retryPolicy);
        }

        int GetMaxRetries(ReadOnlySettings settings)
        {
            var retriesConfig = settings.GetConfigSection<TransportConfig>();

            if (retriesConfig == null)
            {
                return 5;
            }

            return retriesConfig.MaxRetries;
        }
    }
}