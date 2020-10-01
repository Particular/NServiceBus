namespace NServiceBus.AcceptanceTesting.AcceptanceTestingPersistence.TimeoutPersister
{
    using System;
    using Features;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Used to configure in memory timeout persistence.
    /// </summary>
    public class AcceptanceTestingTimeoutPersistence : Feature
    {
        internal AcceptanceTestingTimeoutPersistence()
        {
            DependsOn<TimeoutManager>();
        }

        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Services.AddSingleton(_ => new AcceptanceTestingTimeoutPersister(() => DateTime.UtcNow));
        }
    }
}