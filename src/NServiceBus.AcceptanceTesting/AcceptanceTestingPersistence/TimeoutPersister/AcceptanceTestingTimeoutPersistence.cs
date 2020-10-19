namespace NServiceBus.AcceptanceTesting
{
    using System;
    using Features;
    using Timeout.Core;
    using Microsoft.Extensions.DependencyInjection;

    class AcceptanceTestingTimeoutPersistence : Feature
    {
        public AcceptanceTestingTimeoutPersistence()
        {
            DependsOn<TimeoutManager>();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Services.AddSingleton(x => new AcceptanceTestingTimeoutPersister(() => DateTimeOffset.UtcNow));
            context.Services.AddSingleton<IQueryTimeouts>(x => x.GetService<AcceptanceTestingTimeoutPersister>());
            context.Services.AddSingleton<IPersistTimeouts>(x => x.GetService<AcceptanceTestingTimeoutPersister>());
        }
    }
}