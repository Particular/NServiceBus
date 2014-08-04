namespace NServiceBus
{
    using System;
    using NServiceBus.Features;

    /// <summary>
    /// Controls what happens when a critical error occurs.
    /// </summary>
    public class CriticalErrorFeature : Feature
    {
        /// <summary>
        /// Initializes a enw instance of <see cref="CriticalErrorFeature"/>.
        /// </summary>
        public CriticalErrorFeature()
        {
            EnableByDefault();
        }
        /// <summary>
        /// <see cref="Feature.Setup"/>.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            Action<string, Exception> onCriticalErrorAction;
            if (!context.Settings.TryGet("onCriticalErrorAction", out onCriticalErrorAction))
            {
                onCriticalErrorAction = (errorMessage, exception) =>
                {
                    if (!Configure.BuilderIsConfigured())
                        return;

                    if (!Configure.Instance.Configurer.HasComponent<IBus>())
                        return;

                    Configure.Instance.Builder.Build<IStartableBus>()
                        .Shutdown();
                };
            }
            context.Container.ConfigureComponent(() => new CriticalErrorHandler(onCriticalErrorAction), DependencyLifecycle.SingleInstance);
        }
    }
}