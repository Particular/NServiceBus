namespace NServiceBus.Features
{
    using NServiceBus.Config;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Pipeline;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    class DelayedDeliveryFeature : Feature
    {
        public DelayedDeliveryFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var transportHasNativeDelayedDelivery = context.DoesTransportSupportConstraint<DelayedDeliveryConstraint>();
            var timeoutMgrDisabled = IsTimeoutManagerDisabled(context);

            if (!transportHasNativeDelayedDelivery)
            {
                if (timeoutMgrDisabled)
                {
                    DoNotClearTimeouts(context);
                    context.Pipeline.Register<ThrowIfCannotDeferMessageBehavior.Registration>();
                }
                else
                {
                    var timeoutManagerAddress = GetTimeoutManagerAddress(context);

                    context.Pipeline.Register<RouteDeferredMessageToTimeoutManagerBehavior.Registration>();

                    context.Container.ConfigureComponent(b => new RouteDeferredMessageToTimeoutManagerBehavior(timeoutManagerAddress), DependencyLifecycle.SingleInstance);

                    context.Container.ConfigureComponent(b =>
                    {
                        var pipelinesCollection = context.Settings.Get<PipelineConfiguration>();

                        var dispatchPipeline = new PipelineBase<DispatchContext>(b, context.Settings, pipelinesCollection.MainPipeline);

                        return new RequestCancelingOfDeferredMessagesFromTimeoutManager(timeoutManagerAddress, dispatchPipeline);
                    }, DependencyLifecycle.SingleInstance);
                }
            }
            else
            {
                DoNotClearTimeouts(context);

            }

            context.Pipeline.Register("ApplyDelayedDeliveryConstraint", typeof(ApplyDelayedDeliveryConstraintBehavior), "Applied relevant delayed delivery constraints requested by the user");
        }

        static bool IsTimeoutManagerDisabled(FeatureConfigurationContext context)
        {
            FeatureState timeoutMgrState;
            if (context.Settings.TryGet("NServiceBus.Features.TimeoutManager", out timeoutMgrState))
            {
                return timeoutMgrState == FeatureState.Deactivated || timeoutMgrState == FeatureState.Disabled;
            }
            return true;
        }

        static string GetTimeoutManagerAddress(FeatureConfigurationContext context)
        {
            var unicastConfig = context.Settings.GetConfigSection<UnicastBusConfig>();

            if (unicastConfig != null && !string.IsNullOrWhiteSpace(unicastConfig.TimeoutManagerAddress))
            {
                return unicastConfig.TimeoutManagerAddress;
            }
            var selectedTransportDefinition = context.Settings.Get<TransportDefinition>();
            return selectedTransportDefinition.GetSubScope(context.Settings.Get<string>("MasterNode.Address"), "Timeouts");
        }

        static void DoNotClearTimeouts(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent(b => new NoOpCanceling(), DependencyLifecycle.SingleInstance);
        }
    }
}