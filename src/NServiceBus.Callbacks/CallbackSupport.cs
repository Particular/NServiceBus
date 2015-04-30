namespace NServiceBus.Features
{
    using System;
    using NServiceBus.Callbacks;

    class CallbackSupport : Feature
    {
        public CallbackSupport()
        {
            EnableByDefault();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Settings.Get<Conventions>().AddSystemMessagesConventions(IsLegacyEnumResponse);
            context.Container.ConfigureComponent<RequestResponseMessageLookup>(DependencyLifecycle.SingleInstance);
            context.Pipeline.Register<RequestResponseInvocationBehavior.Registration>();
            context.Pipeline.Register<UpdateRequestResponseCorrelationTableBehavior.Registration>();
            context.Pipeline.Register<ConvertLegacyEnumResponseToLegacyControlMessageBehavior.Registration>();
            context.Pipeline.Register<SetLegacyReturnCodeBehavior.Registration>();
        }

        internal static bool IsLegacyEnumResponse(Type instanceType)
        {
            return instanceType.IsGenericType
                   && instanceType.GetGenericTypeDefinition() == typeof(LegacyEnumResponse<>);
        }
    }
}