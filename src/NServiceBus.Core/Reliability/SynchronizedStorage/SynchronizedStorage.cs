namespace NServiceBus.Persistence
{
    using Features;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    ///
    /// </summary>
    public class SynchronizedStorage : Feature
    {
        /// <summary>
        ///
        /// </summary>
        public SynchronizedStorage()
        {
            EnableByDefault();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="context"></param>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Services.AddScoped<SynchronizedStorageSessionProvider>();
            context.Services.AddTransient<ISynchronizedStorageSessionProvider>(provider =>
                provider.GetService<SynchronizedStorageSessionProvider>());
        }
    }
}