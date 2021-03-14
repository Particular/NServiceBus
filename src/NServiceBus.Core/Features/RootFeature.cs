namespace NServiceBus.Features
{
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// A root feature that is always enabled.
    /// </summary>
    class RootFeature : Feature
    {
        public RootFeature()
        {
            EnableByDefault();
        }

        protected internal override Task Setup(FeatureConfigurationContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}