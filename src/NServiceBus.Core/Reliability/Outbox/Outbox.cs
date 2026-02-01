namespace NServiceBus.Features;

using System;
using ConsistencyGuarantees;
using NServiceBus.Settings;
using Transport;

/// <summary>
/// Configure the Outbox.
/// </summary>
public sealed class Outbox : Feature
{
    /// <summary>
    /// Creates a new instance of the outbox feature.
    /// </summary>
    public Outbox()
    {
        Defaults(s => s.SetDefault(TimeToKeepDeduplicationEntries, TimeSpan.FromDays(5)));

        Prerequisite(context => ReceivingEnabled(context.Settings) || AllowUseWithoutReceiving(context.Settings),
            "Outbox is only relevant for endpoints receiving messages.");

        Prerequisite(context => !ReceivingEnabled(context.Settings) || TransactionsEnabled(context.Settings),
            "Outbox isn't needed since the receive transactions have been turned off");

        Enable<SynchronizedStorage>();

        DependsOn<SynchronizedStorage>();
    }

    static bool ReceivingEnabled(IReadOnlySettings settings) => !settings.GetOrDefault<bool>("Endpoint.SendOnly");

    static bool TransactionsEnabled(IReadOnlySettings settings) => settings.GetRequiredTransactionModeForReceives() != TransportTransactionMode.None;

    static bool AllowUseWithoutReceiving(IReadOnlySettings settings) => settings.GetOrDefault<bool>("Outbox.AllowUseWithoutReceiving");

    /// <summary>
    /// See <see cref="Feature.Setup" />.
    /// </summary>
    protected override void Setup(FeatureConfigurationContext context)
    {
        if (!context.HasSupportForStorage<StorageType.Outbox>())
        {
            throw new Exception("The selected persistence doesn't have support for outbox storage. Select another persistence or disable the outbox feature using endpointConfiguration.DisableFeature<Outbox>()");
        }

        if (!ReceivingEnabled(context.Settings))
        {
            return;
        }

        // We cannot allow the transport to operate in SendsWithAtomicReceive because a transport
        // might then only release the outgoing operations when the incoming transport transaction is committed meaning
        // the actual sends would happen after we have set the outbox record as dispatched and not as part of
        // TransportReceiveToPhysicalMessageConnector fork into the batched dispatched phase. Should acknowledging
        // the incoming operation fail and the message be retried we would already have cleared the outbox record's
        // transport operations leading to outgoing message loss. ReceiveOnly mode prevents outgoing messages from
        // being enlisted in the receive transaction, so there's no need to force DispatchConsistency.Isolated.
        if (context.Settings.GetRequiredTransactionModeForReceives() != TransportTransactionMode.ReceiveOnly)
        {
            throw new Exception(
                $"Outbox requires transport to be running in `{nameof(TransportTransactionMode.ReceiveOnly)}` mode. Use the `{nameof(TransportDefinition.TransportTransactionMode)}` property on the transport definition to specify the transaction mode.");
        }

        //note: in the future we should change the persister api to give us a "outbox factory" so that we can register it in DI here instead of relying on the persister to do it
    }

    internal const string TimeToKeepDeduplicationEntries = "Outbox.TimeToKeepDeduplicationEntries";
}
