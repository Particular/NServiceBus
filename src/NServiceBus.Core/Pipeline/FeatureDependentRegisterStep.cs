namespace NServiceBus.Pipeline
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.Settings;

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class FeatureDependentRegisterStep<T> : RegisterStep
        where T : Feature
    {
        /// <summary>
        /// 
        /// </summary>
        protected FeatureDependentRegisterStep(string stepId, Type behavior, string description) : base(stepId, behavior, description)
        {
        }

        /// <summary>
        /// Checks if this behavior is enabled.
        /// </summary>
        public override bool IsEnabled(ReadOnlySettings settings)
        {
            var enabled = settings.IsFeatureActive(typeof(T));
            return enabled;
        }
    }
}