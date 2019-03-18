namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Unicast;

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

            if (!context.Settings.TryGet("NServiceBus.ExecuteTheseHandlersFirst", out List<Type> order))
            {
                order = new List<Type>(0);
            }

            LoadMessageHandlers(context, order);
        }

        static void LoadMessageHandlers(FeatureConfigurationContext context, List<Type> orderedTypes)
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
            var handlerRegistry = new MessageHandlerRegistry();

            foreach (var t in types.Where(IsMessageHandler))
            {
                context.Container.ConfigureComponent(t, DependencyLifecycle.InstancePerUnitOfWork);
                handlerRegistry.RegisterHandler(t);
            }

            context.Container.RegisterSingleton(handlerRegistry);
        }

        public static bool IsMessageHandler(Type type)
        {
            if (type.IsAbstract || type.IsGenericTypeDefinition)
            {
                return false;
            }

            return type.GetInterfaces()
                .Where(@interface => @interface.IsGenericType)
                .Select(@interface => @interface.GetGenericTypeDefinition())
                .Any(genericTypeDef => genericTypeDef == IHandleMessagesType);
        }

        static Type IHandleMessagesType = typeof(IHandleMessages<>);
    }
}