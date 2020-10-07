namespace NServiceBus.AcceptanceTesting.AcceptanceTestingPersistence.TimeoutPersister
{
    using System;
    using Features;
    using Microsoft.Extensions.DependencyInjection;

    class AcceptanceTestingTimeoutPersistence : Feature
    {
        internal AcceptanceTestingTimeoutPersistence()
        {
            DependsOn<TimeoutManager>();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Services.AddSingleton(_ => new AcceptanceTestingTimeoutPersister(() => DateTime.UtcNow));
        }
    }
}