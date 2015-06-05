namespace NServiceBus.Pipeline
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.Settings;

    /// <summary>
    /// 
    /// </summary>
    public abstract class FeatureDependentRegisterStep<T> : RegisterStep
        where T : Feature
    {
        /// <summary>
        /// Initializes a new insatnce of <see cref="FeatureDependentRegisterStep{T}"/>.
        /// </summary>
        protected FeatureDependentRegisterStep(string stepId, Type behavior, string description) : base(stepId, behavior, description)
        {
        }

        /// <summary>
        /// Checks if this behavior is enabled.
        /// </summary>
        public override bool IsEnabled(ReadOnlySettings settings)
        {
            return settings.IsFeatureActive(typeof(T));
        }
    }
}