namespace NServiceBus.Features
{
    using Microsoft.Extensions.DependencyInjection;
    using Persistence;

    /// <summary>
    /// Configures the synchronized storage.
    /// </summary>
    public class SynchronizedStorage : Feature
    {
        internal SynchronizedStorage() { }

        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Services.AddScoped<SynchronizedStorageSessionProvider>();
            context.Services.AddTransient<ISynchronizedStorageSessionProvider>(provider => provider.GetService<SynchronizedStorageSessionProvider>());
        }
    }
}