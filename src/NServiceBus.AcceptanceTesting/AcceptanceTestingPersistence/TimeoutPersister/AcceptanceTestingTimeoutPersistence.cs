namespace NServiceBus.AcceptanceTesting
{
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
            context.Services.AddSingleton<IQueryTimeouts, AcceptanceTestingTimeoutPersister>();
        }
    }
}