namespace NServiceBus.Features
{
    using System;

    /// <summary>
    /// Controls what happens when a critical error occurs.
    /// </summary>
    class CriticalErrorHandling : Feature
    {
        /// <summary>
        /// Initializes a enw instance of <see cref="CriticalErrorHandling"/>.
        /// </summary>
        internal CriticalErrorHandling()
        {
            EnableByDefault();
        }


        /// <summary>
        /// <see cref="Feature.Setup"/>.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            Action<string, Exception> errorAction;
            context.Settings.TryGet("onCriticalErrorAction", out errorAction);
            context.Container.ConfigureComponent(builder => new CriticalError(errorAction, builder), DependencyLifecycle.SingleInstance);
        }

    }
}