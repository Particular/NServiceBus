namespace NServiceBus.Features
{
    using System;
    using NServiceBus.Callbacks;

    class CallbacksSupport : Feature
    {
        public CallbacksSupport()
        {
            EnableByDefault();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Settings.Get<Conventions>().AddSystemMessagesConventions(isLegacyEnumResponse);
            context.Container.ConfigureComponent<RequestResponseMessageLookup>(DependencyLifecycle.SingleInstance);
            context.Pipeline.Register<RequestResponseInvocationBehavior.Registration>();
            context.Pipeline.Register<UpdateRequestResponseCorrelationTableBehavior.Registration>();
            context.Pipeline.Register<ConvertLegacyEnumResponseToLegacyControlMessageBehavior.Registration>();
            context.Pipeline.Register<CaptureOutgoingLogicalMessageInstanceBehavior.Registration>();
        }

        internal static bool isLegacyEnumResponse(Type instanceType)
        {
            return instanceType.IsGenericType
                   && instanceType.GetGenericTypeDefinition() == typeof(LegacyEnumResponse<>);
        }
    }
}