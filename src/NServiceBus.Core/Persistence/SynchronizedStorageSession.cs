namespace NServiceBus.Persistence
{
    using Features;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Represents a storage session.
    /// </summary>
    public interface ISynchronizedStorageSession
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public class SynchronizedStorageSessionProvider : ISynchronizedStorageSessionProvider
    {
        /// <summary>
        /// 
        /// </summary>
        public ISynchronizedStorageSession SynchronizedStorageSession { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public interface ISynchronizedStorageSessionProvider
    {
        /// <summary>
        /// 
        /// </summary>
        ISynchronizedStorageSession SynchronizedStorageSession { get; }
    }

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
