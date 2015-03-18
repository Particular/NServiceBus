namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using NServiceBus.Logging;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;
    using NServiceBus.Unicast;

    class RegisterHandlersInOrder : Feature
    {
        public RegisterHandlersInOrder()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            if (context.Container.HasComponent<MessageHandlerRegistry>())
            {
                return;
            }

            IEnumerable<Type> order;

            if (!context.Settings.TryGet("LoadMessageHandlers.Order.Types", out order))
            {
                order = ISpecifyMessageHandlerOrdering(context.Settings);
            }

            LoadMessageHandlers(context, order);
        }

        static IEnumerable<Type> ISpecifyMessageHandlerOrdering(ReadOnlySettings settings)
        {
            var types = new List<Type>();

            foreach (var t in settings.GetAvailableTypes().Where(TypeSpecifiesMessageHandlerOrdering))
            {
                Logger.DebugFormat("Going to ask for message handler ordering from '{0}'.", t);

                var order = new Order();
                ((ISpecifyMessageHandlerOrdering)Activator.CreateInstance(t)).SpecifyOrder(order);

                foreach (var ht in order.Types)
                {
                    if (types.Contains(ht))
                    {
                        throw new ConfigurationErrorsException(string.Format("The order in which the type '{0}' should be invoked was already specified by a previous implementor of ISpecifyMessageHandlerOrdering. Check the debug logs to see which other specifiers have been invoked.", ht));
                    }
                }

                types.AddRange(order.Types);
            }

            return types;
        }

        static bool TypeSpecifiesMessageHandlerOrdering(Type t)
        {
            return typeof(ISpecifyMessageHandlerOrdering).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface;
        }

        static void LoadMessageHandlers(FeatureConfigurationContext context, IEnumerable<Type> orderedTypes)
        {
            var types = new List<Type>(context.Settings.GetAvailableTypes());

            foreach (var t in orderedTypes)
            {
                types.Remove(t);
            }

            types.InsertRange(0, orderedTypes);

            ConfigureMessageHandlersIn(context, types);
        }

        static void ConfigureMessageHandlersIn(FeatureConfigurationContext context, IEnumerable<Type> types)
        {
            var handlerRegistry = new MessageHandlerRegistry(context.Settings.Get<Conventions>());
            var handlers = new List<Type>();

            foreach (var t in types.Where(IsMessageHandler))
            {
                context.Container.ConfigureComponent(t, DependencyLifecycle.InstancePerUnitOfWork);
                handlerRegistry.RegisterHandler(t);
                handlers.Add(t);
            }

            List<Action<IConfigureComponents>> propertiesToInject;
            if (context.Settings.TryGet("NServiceBus.HandlerProperties", out propertiesToInject))
            {
                foreach (var action in propertiesToInject)
                {
                    action(context.Container);
                }
            }

            context.Container.RegisterSingleton(handlerRegistry);
        }

        public static bool IsMessageHandler(Type type)
        {
            if (type.IsAbstract || type.IsGenericTypeDefinition)
            {
                return false;
            }

            return type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == IHandleMessagesType);
        }

        static Type IHandleMessagesType = typeof(IHandleMessages<>);
        static ILog Logger = LogManager.GetLogger<RegisterHandlersInOrder>();
    }
}