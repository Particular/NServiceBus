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
            context.Container.ConfigureComponent<RequestResponseStateLookup>(DependencyLifecycle.SingleInstance);
            context.MainPipeline.Register<RequestResponseInvocationBehavior.Registration>();
            context.MainPipeline.Register<UpdateRequestResponseCorrelationTableBehavior.Registration>();
            context.MainPipeline.Register<SetLegacyReturnCodeBehavior.Registration>();
        }

        internal static bool IsLegacyEnumResponse(Type instanceType)
        {
            return instanceType.IsGenericType
                   && instanceType.GetGenericTypeDefinition() == typeof(LegacyEnumResponse<>);
        }
    }
}