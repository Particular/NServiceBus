namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Unicast;
    using NServiceBus.Unicast.Messages;
    using NServiceBus.Unicast.Transport;

    class UnicastBus : Feature
    {
        internal UnicastBus()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<BusNotifications>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<BehaviorContextStacker>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => b.Build<BehaviorContextStacker>().GetCurrentOrRootContext(), DependencyLifecycle.InstancePerCall);

            //Hack because we can't register as IStartableBus because it would automatically register as IBus and overrode the proper IBus registration.
            context.Container.ConfigureComponent<UnicastBusInternal>(DependencyLifecycle.SingleInstance);

            var knownMessages = context.Settings.GetAvailableTypes()
                .Where(context.Settings.Get<Conventions>().IsMessageType)
                .ToList();

            ConfigureMessageRegistry(context, knownMessages);

            if (context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                return;
            }

            HardcodedPipelineSteps.RegisterIncomingCoreBehaviors(context.Pipeline);

            var transactionSettings = new TransactionSettings(context.Settings);

            if (transactionSettings.DoNotWrapHandlersExecutionInATransactionScope)
            {
                context.Pipeline.Register<SuppressAmbientTransactionBehavior.Registration>();
            }
            else
            {
                context.Pipeline.Register<HandlerTransactionScopeWrapperBehavior.Registration>();
            }
        }

        static void ConfigureMessageRegistry(FeatureConfigurationContext context, IEnumerable<Type> knownMessages)
        {
            var messageRegistry = new MessageMetadataRegistry(context.Settings.Get<Conventions>());

            foreach (var msg in knownMessages)
            {
                messageRegistry.RegisterMessageType(msg);
            }

            context.Container.RegisterSingleton(messageRegistry);
            context.Container.ConfigureComponent<LogicalMessageFactory>(DependencyLifecycle.SingleInstance);

            if (!Logger.IsInfoEnabled)
            {
                return;
            }

            var messageDefinitions = messageRegistry.GetAllMessages().ToList();

            Logger.DebugFormat("Number of messages found: {0}", messageDefinitions.Count());

            if (!Logger.IsDebugEnabled)
            {
                return;
            }

            Logger.DebugFormat("Message definitions: \n {0}",
                string.Concat(messageDefinitions.Select(md => md.ToString() + "\n")));
        }

        static ILog Logger = LogManager.GetLogger<UnicastBus>();
    }
}