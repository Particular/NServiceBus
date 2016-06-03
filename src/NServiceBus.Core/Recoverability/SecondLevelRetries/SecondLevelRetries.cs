namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using Config;
    using ConsistencyGuarantees;
    using Settings;

    /// <summary>
    /// Used to configure Second Level Retries.
    /// </summary>
    public class SecondLevelRetries : Feature
    {
        internal SecondLevelRetries()
        {
            EnableByDefault();

            DependsOn<DelayedDeliveryFeature>();
            DependsOn<StoreFaultsInErrorQueue>();

            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Send only endpoints can't use SLR since it requires receive capabilities");

            Prerequisite(IsEnabledInConfig, "SLR was disabled in config");

            Prerequisite(context => context.Settings.GetRequiredTransactionModeForReceives() != TransportTransactionMode.None, "Transactions must be enabled since SLR requires the transport to be able to rollback");
        }

        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var retryPolicy = GetRetryPolicy(context.Settings);

            context.Container.RegisterSingleton(typeof(SecondLevelRetryPolicy), retryPolicy);

            context.Pipeline.Register<SecondLevelRetriesBehavior.Registration>();

            //context.Container.ConfigureComponent(b => new SecondLevelRetriesBehavior(retryPolicy, context.Settings.LocalAddress(), b.Build<FailureInfoStorage>()), DependencyLifecycle.InstancePerCall);
        }

        bool IsEnabledInConfig(FeatureConfigurationContext context)
        {
            var retriesConfig = context.Settings.GetConfigSection<SecondLevelRetriesConfig>();

            if (retriesConfig == null)
            {
                return true;
            }

            if (retriesConfig.NumberOfRetries == 0)
            {
                return false;
            }

            return retriesConfig.Enabled;
        }

        static SecondLevelRetryPolicy GetRetryPolicy(ReadOnlySettings settings)
        {
            var customRetryPolicy = settings.GetOrDefault<Func<Dictionary<string, string>, TimeSpan>>("SecondLevelRetries.RetryPolicy");

            if (customRetryPolicy != null)
            {
                return new CustomSecondLevelRetryPolicy(customRetryPolicy);
            }

            var retriesConfig = settings.GetConfigSection<SecondLevelRetriesConfig>();
            if (retriesConfig != null)
            {
                return new DefaultSecondLevelRetryPolicy(retriesConfig.NumberOfRetries, retriesConfig.TimeIncrease);
            }

            return new DefaultSecondLevelRetryPolicy(DefaultSecondLevelRetryPolicy.DefaultNumberOfRetries, DefaultSecondLevelRetryPolicy.DefaultTimeIncrease);
        }
    }
}