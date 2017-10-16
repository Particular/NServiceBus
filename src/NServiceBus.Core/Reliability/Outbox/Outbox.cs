namespace NServiceBus.Features
{
    using System;
    using ConsistencyGuarantees;

    /// <summary>
    /// Configure the Outbox.
    /// </summary>
    public class Outbox : Feature
    {
        internal Outbox()
        {
            Defaults(s => s.SetDefault(InMemoryOutboxPersistence.TimeToKeepDeduplicationEntries, TimeSpan.FromDays(5)));
            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"),
                "Outbox is only relevant for endpoints receiving messages.");
            Prerequisite(c => !c.Settings.GetOrDefault<bool>("Endpoint.SendOnly")
                && c.Settings.GetRequiredTransactionModeForReceives() != TransportTransactionMode.None,
                "Outbox isn't needed since the receive transactions have been turned off");
        }

        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            if (!PersistenceStartup.HasSupportFor<StorageType.Outbox>(context.Settings))
            {
                throw new Exception("The selected persistence doesn't have support for outbox storage. Select another persistence or disable the outbox feature using endpointConfiguration.DisableFeature<Outbox>()");
            }

            //note: in the future we should change the persister api to give us a "outbox factory" so that we can register it in DI here instead of relying on the persister to do it
            context.Pipeline.Register("ForceBatchDispatchToBeIsolated", new ForceBatchDispatchToBeIsolatedBehavior(), "Makes sure that we dispatch straight to the transport so that we can safely set the outbox record to dispatched one the dispatch pipeline returns.");
        }
    }
}