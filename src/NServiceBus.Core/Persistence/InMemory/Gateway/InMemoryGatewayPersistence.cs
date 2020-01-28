namespace NServiceBus.Features
{
    using Gateway.Deduplication;

    /// <summary>
    /// In-memory Gateway.
    /// </summary>
    public class InMemoryGatewayPersistence : Feature
    {
        internal InMemoryGatewayPersistence()
        {
            DependsOn("NServiceBus.Features.Gateway");
            Defaults(s =>
            {
                s.SetDefault(MaxSizeKey, MaxSizeDefault);
            });
        }

        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var maxSize = context.Settings.Get<int>(MaxSizeKey);
            context.Container.RegisterSingleton<IDeduplicateMessages>(new InMemoryGatewayDeduplication(new ClientIdStorage(maxSize)));
        }

        internal const string MaxSizeKey = "InMemoryGatewayDeduplication.MaxSize";
        const int MaxSizeDefault = 10000;
    }
}