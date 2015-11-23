namespace NServiceBus.Features
{
    using System;
    using NServiceBus.Config;
    using NServiceBus.Pipeline;
    using NServiceBus.Recoverability.SecondLevelRetries;
    using NServiceBus.Settings;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    /// <summary>
    /// Used to configure Second Level Retries.
    /// </summary>
    public class SecondLevelRetries : Feature
    {
        internal SecondLevelRetries()
        {
            EnableByDefault();

            DependsOn<DelayedDeliveryFeature>();

            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Send only endpoints can't use SLR since it requires receive capabilities");

            Prerequisite(IsEnabledInConfig, "SLR was disabled in config");
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var  retryPolicy = GetRetryPolicy(context.Settings);

            context.Container.RegisterSingleton(typeof(SecondLevelRetryPolicy), retryPolicy);
            context.Pipeline.Register<SecondLevelRetriesBehavior.Registration>();


            context.Container.ConfigureComponent(b =>
            {
                var routingPipe = b.Build<IPipeInlet<RoutingContext>>();
                return new SecondLevelRetriesBehavior(routingPipe, retryPolicy, b.Build<BusNotifications>(), context.Settings.LocalAddress());
            }, DependencyLifecycle.InstancePerCall);
        }

        bool IsEnabledInConfig(FeatureConfigurationContext context)
        {
            var retriesConfig = context.Settings.GetConfigSection<SecondLevelRetriesConfig>();

            if (retriesConfig == null)
                return true;

            if (retriesConfig.NumberOfRetries == 0)
                return false;

            return retriesConfig.Enabled;
        }

        static SecondLevelRetryPolicy GetRetryPolicy(ReadOnlySettings settings)
        {
            var customRetryPolicy = settings.GetOrDefault<Func<IncomingMessage, TimeSpan>>("SecondLevelRetries.RetryPolicy");

            if (customRetryPolicy != null)
            {
                return new CustomSecondLevelRetryPolicy(customRetryPolicy);
            }

            var retriesConfig = settings.GetConfigSection<SecondLevelRetriesConfig>();
            if (retriesConfig != null)
            {
                return new DefaultSecondLevelRetryPolicy(retriesConfig.NumberOfRetries, retriesConfig.TimeIncrease);
            }

            return new DefaultSecondLevelRetryPolicy(DefaultSecondLevelRetryPolicy.DefaultNumberOfRetries,DefaultSecondLevelRetryPolicy.DefaultTimeIncrease);
        }
    }
}