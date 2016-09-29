namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Config;
    using ConsistencyGuarantees;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Faults;
    using Features;
    using Hosting;
    using Logging;
    using Routing;
    using Settings;
    using Support;
    using Transport;

    class Recoverability : Feature
    {
        public Recoverability()
        {
            EnableByDefault();
            DependsOnOptionally<DelayedDeliveryFeature>();

            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"),
                "Message recoverability is only relevant for endpoints receiving messages.");
            Defaults(settings =>
            {
                settings.SetDefault(NumberOfDelayedRetries, DefaultNumberOfRetries);
                settings.SetDefault(DelayedRetriesTimeIncrease, DefaultTimeIncrease);
                settings.SetDefault(NumberOfImmediateRetries, 5);
                settings.SetDefault(FaultHeaderCustomization, new Action<Dictionary<string, string>>(headers => { }));
            });
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var errorQueue = context.Settings.ErrorQueueAddress();
            context.Settings.Get<QueueBindings>().BindSending(errorQueue);

            context.Container.ConfigureComponent(b =>
            {
                Func<string, MoveToErrorsExecutor> moveToErrorsExecutorFactory = localAddress =>
                {
                    var hostInfo = b.Build<HostInformation>();
                    var staticFaultMetadata = new Dictionary<string, string>
                    {
                        {FaultsHeaderKeys.FailedQ, localAddress},
                        {Headers.ProcessingMachine, RuntimeEnvironment.MachineName},
                        {Headers.ProcessingEndpoint, context.Settings.EndpointName()},
                        {Headers.HostId, hostInfo.HostId.ToString("N")},
                        {Headers.HostDisplayName, hostInfo.DisplayName}
                    };

                    var headerCustomizations = context.Settings.Get<Action<Dictionary<string, string>>>(FaultHeaderCustomization);

                    return new MoveToErrorsExecutor(b.Build<IDispatchMessages>(), staticFaultMetadata, headerCustomizations);
                };

                var transactionsOn = context.Settings.GetRequiredTransactionModeForReceives() != TransportTransactionMode.None;
                var delayedRetryConfig = GetDelayedRetryConfig(context.Settings, transactionsOn);
                var delayedRetriesAvailable = transactionsOn
                                              && (context.Settings.DoesTransportSupportConstraint<DelayedDeliveryConstraint>() || context.Settings.Get<TimeoutManagerAddressConfiguration>().TransportAddress != null);

                Func<string, DelayedRetryExecutor> delayedRetryExecutorFactory = localAddress =>
                {
                    if (delayedRetriesAvailable)
                    {
                        return new DelayedRetryExecutor(
                            localAddress,
                            b.Build<IDispatchMessages>(),
                            context.Settings.DoesTransportSupportConstraint<DelayedDeliveryConstraint>()
                                ? null
                                : context.Settings.Get<TimeoutManagerAddressConfiguration>().TransportAddress);
                    }

                    return null;
                };

                var immediateRetryConfig = GetImmediateRetryConfig(context.Settings, transactionsOn);
                var immediateRetriesAvailable = transactionsOn;

                Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> policy;
                if (!context.Settings.TryGet(PolicyOverride, out policy))
                {
                    policy = DefaultRecoverabilityPolicy.Invoke;
                }

                return new RecoverabilityExecutorFactory(
                    policy,
                    new RecoverabilityConfig(immediateRetryConfig, delayedRetryConfig, new FailedConfig(errorQueue)),
                    delayedRetryExecutorFactory,
                    moveToErrorsExecutorFactory,
                    immediateRetriesAvailable,
                    delayedRetriesAvailable);

            }, DependencyLifecycle.SingleInstance);

            RaiseLegacyNotifications(context);

            //HINT: we turn off the legacy retries satellite only when explicitly configured by the user
            bool disableLegacyRetriesSatellite;
            if (context.Settings.TryGet(DisableLegacyRetriesSatellite, out disableLegacyRetriesSatellite) == false)
            {
                SetupLegacyRetriesSatellite(context);
            }
        }

        static ImmediateConfig GetImmediateRetryConfig(ReadOnlySettings settings, bool transactionsOn)
        {
            if (!transactionsOn)
            {
                Logger.Warn("Immediate Retries will be disabled. Immediate Retries are not supported when running with TransportTransactionMode.None. Failed messages will be moved to the error queue instead.");
                //Transactions must be enabled since Immediate Retries requires the transport to be able to rollback
                return new ImmediateConfig(0);
            }

            var retriesConfig = settings.GetConfigSection<TransportConfig>();
            var maxImmediateRetries = retriesConfig?.MaxRetries ?? settings.Get<int>(NumberOfImmediateRetries);

            return new ImmediateConfig(maxImmediateRetries);
        }

        static DelayedConfig GetDelayedRetryConfig(ReadOnlySettings settings, bool transactionsOn)
        {
            if (!transactionsOn)
            {
                Logger.Warn("Delayed Retries will be disabled. Delayed retries are not supported when running with TransportTransactionMode.None. Failed messages will be moved to the error queue instead.");
                //Transactions must be enabled since Delayed Retries requires the transport to be able to rollback
                return new DelayedConfig(0, TimeSpan.Zero);
            }

            var numberOfRetries = settings.Get<int>(NumberOfDelayedRetries);
            var timeIncrease = settings.Get<TimeSpan>(DelayedRetriesTimeIncrease);

            var retriesConfig = settings.GetConfigSection<SecondLevelRetriesConfig>();
            if (retriesConfig != null)
            {
                numberOfRetries = retriesConfig.Enabled ? retriesConfig.NumberOfRetries : 0;
                timeIncrease = retriesConfig.TimeIncrease;
            }

            return new DelayedConfig(numberOfRetries, timeIncrease);
        }

        //note: will soon be removed since we're deprecating Notifications in favor of the new notifications
        static void RaiseLegacyNotifications(FeatureConfigurationContext context)
        {
            var legacyNotifications = context.Settings.Get<Notifications>();
            var notifications = context.Settings.Get<NotificationSubscriptions>();

            notifications.Subscribe<MessageToBeRetried>(e =>
            {
                if (e.IsImmediateRetry)
                {
                    legacyNotifications.Errors.InvokeMessageHasFailedAnImmediateRetryAttempt(e.Attempt, e.Message, e.Exception);
                }
                else
                {
                    legacyNotifications.Errors.InvokeMessageHasBeenSentToDelayedRetries(e.Attempt, e.Message, e.Exception);
                }

                return TaskEx.CompletedTask;
            });

            notifications.Subscribe<MessageFaulted>(e =>
            {
                legacyNotifications.Errors.InvokeMessageHasBeenSentToErrorQueue(e.Message, e.Exception, e.ErrorQueue);
                return TaskEx.CompletedTask;
            });
        }

        void SetupLegacyRetriesSatellite(FeatureConfigurationContext context)
        {
            var retriesQueueLogicalAddress = context.Settings.LogicalAddress().CreateQualifiedAddress("Retries");
            var retriesQueueTransportAddress = context.Settings.GetTransportAddress(retriesQueueLogicalAddress);

            var mainQueueLogicalAddress = context.Settings.LogicalAddress();
            var mainQueueTransportAddress = context.Settings.GetTransportAddress(mainQueueLogicalAddress);

            var requiredTransactionMode = context.Settings.GetRequiredTransactionModeForReceives();

            context.AddSatelliteReceiver("Legacy Retries Processor", retriesQueueTransportAddress, requiredTransactionMode, new PushRuntimeSettings(maxConcurrency: 1),
                (config, errorContext) =>
                {
                    return RecoverabilityAction.MoveToError(config.Failed.ErrorQueue);
                },
                (builder, messageContext) =>
                {
                    var messageDispatcher = builder.Build<IDispatchMessages>();

                    var outgoingHeaders = messageContext.Headers;
                    outgoingHeaders.Remove("NServiceBus.ExceptionInfo.Reason");
                    outgoingHeaders.Remove("NServiceBus.ExceptionInfo.ExceptionType");
                    outgoingHeaders.Remove("NServiceBus.ExceptionInfo.InnerExceptionType");
                    outgoingHeaders.Remove("NServiceBus.ExceptionInfo.HelpLink");
                    outgoingHeaders.Remove("NServiceBus.ExceptionInfo.Message");
                    outgoingHeaders.Remove("NServiceBus.ExceptionInfo.Source");
                    outgoingHeaders.Remove("NServiceBus.FailedQ");
                    outgoingHeaders.Remove("NServiceBus.TimeOfFailure");

                    //HINT: this header is added by v3 when doing SLR
                    outgoingHeaders.Remove("NServiceBus.OriginalId");

                    var outgoingMessage = new OutgoingMessage(messageContext.MessageId, outgoingHeaders, messageContext.Body);
                    var outgoingOperation = new TransportOperation(outgoingMessage, new UnicastAddressTag(mainQueueTransportAddress));

                    return messageDispatcher.Dispatch(new TransportOperations(outgoingOperation), messageContext.TransportTransaction, messageContext.Context);
                });
        }

        public const string NumberOfDelayedRetries = "Recoverability.Delayed.DefaultPolicy.Retries";
        public const string DelayedRetriesTimeIncrease = "Recoverability.Delayed.DefaultPolicy.Timespan";
        public const string NumberOfImmediateRetries = "Recoverability.Immediate.Retries";
        public const string FaultHeaderCustomization = "Recoverability.Failed.FaultHeaderCustomization";
        public const string PolicyOverride = "Recoverability.CustomPolicy";
        public const string DisableLegacyRetriesSatellite = "Recoverability.DisableLegacyRetriesSatellite";

        static ILog Logger = LogManager.GetLogger<Recoverability>();
        internal static int DefaultNumberOfRetries = 3;
        internal static TimeSpan DefaultTimeIncrease = TimeSpan.FromSeconds(10);
    }
}