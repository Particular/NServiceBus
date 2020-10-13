namespace NServiceBus.Core.Tests.Fakes
{
    using NServiceBus.Features;
    using NServiceBus.Timeout.Core;
    using Microsoft.Extensions.DependencyInjection;

    class FakeTimeoutPersistence : Feature
    {
        public FakeTimeoutPersistence()
        {
            DependsOn<TimeoutManager>();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Services.AddSingleton<IQueryTimeouts, FakeTimeoutPersister>();
        }
    }
}