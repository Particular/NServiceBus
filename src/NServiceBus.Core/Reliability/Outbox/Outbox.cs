﻿namespace NServiceBus.Features
{
    using ConsistencyGuarantees;
    using System;
    using Transport;

    /// <summary>
    /// Configure the Outbox.
    /// </summary>
    public class Outbox : Feature
    {
        internal Outbox()
        {
            Defaults(s =>
            {
                s.EnableFeatureByDefault<SynchronizedStorage>();
                s.SetDefault(TimeToKeepDeduplicationEntries, TimeSpan.FromDays(5));
            });

            DependsOn<SynchronizedStorage>();

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

            // ForceBatchDispatchToBeIsolatedBehavior set the dispatch consistency to isolated which instructs
            // the transport to not enlist the outgoing operation in the incoming message transaction. Unfortunately
            // this is not enough. We cannot allow the transport to operate in SendsWithAtomicReceive because a transport
            // might then only release the outgoing operations when the incoming transport transaction is committed meaning
            // the actual sends would happen after we have set the outbox record as dispatched and not as part of
            // TransportReceiveToPhysicalMessageConnector fork into the batched dispatched phase. Should acknowledging
            // the incoming operation fail and the message be retried we would already have cleared the outbox record's
            // transport operations leading to outgoing message loss.
            if (context.Settings.GetRequiredTransactionModeForReceives() != TransportTransactionMode.ReceiveOnly)
            {
                throw new Exception(
                    $"Outbox requires transport to be running in ${nameof(TransportTransactionMode.ReceiveOnly)} mode. Use ${nameof(TransportDefinition.TransportTransactionMode)} property on the transport definition to specify the transaction mode.");
            }

            //note: in the future we should change the persister api to give us a "outbox factory" so that we can register it in DI here instead of relying on the persister to do it
            context.Pipeline.Register("ForceBatchDispatchToBeIsolated", new ForceBatchDispatchToBeIsolatedBehavior(), "Makes sure that we dispatch straight to the transport so that we can safely set the outbox record to dispatched once the dispatch pipeline returns.");
        }
        internal const string TimeToKeepDeduplicationEntries = "Outbox.TimeToKeepDeduplicationEntries";
    }
}