namespace NServiceBus.Features;

using System;
using ConsistencyGuarantees;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Outbox;
using NServiceBus.Settings;
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
            s.SetDefault(TimeToKeepDeduplicationEntries, TimeSpan.FromDays(5));
            s.EnableFeatureByDefault<SynchronizedStorage>();
        });

        Prerequisite(context => ReceivingEnabled(context.Settings) || AllowUseWithoutReceiving(context.Settings),
            "Outbox is only relevant for endpoints receiving messages.");

        Prerequisite(context => !ReceivingEnabled(context.Settings) || TransactionsEnabled(context.Settings),
            "Outbox isn't needed since the receive transactions have been turned off");

        DependsOn<SynchronizedStorage>();
    }

    static bool ReceivingEnabled(IReadOnlySettings settings) => !settings.GetOrDefault<bool>("Endpoint.SendOnly");

    static bool TransactionsEnabled(IReadOnlySettings settings) => settings.GetRequiredTransactionModeForReceives() != TransportTransactionMode.None;

    static bool AllowUseWithoutReceiving(IReadOnlySettings settings) => settings.GetOrDefault<bool>("Outbox.AllowUseWithoutReceiving");

    static bool AllowSendsAtomicWithReceive(IReadOnlySettings settings) => settings.GetOrDefault<bool>("Outbox.AllowSendsAtomicWithReceive");

    /// <summary>
    /// See <see cref="Feature.Setup" />.
    /// </summary>
    protected internal override void Setup(FeatureConfigurationContext context)
    {
        if (!PersistenceStartup.HasSupportFor<StorageType.Outbox>(context.Settings))
        {
            throw new Exception("The selected persistence doesn't have support for outbox storage. Select another persistence or disable the outbox feature using endpointConfiguration.DisableFeature<Outbox>()");
        }

        if (!ReceivingEnabled(context.Settings))
        {
            return;
        }

        if (context.Settings.GetRequiredTransactionModeForReceives() == TransportTransactionMode.SendsAtomicWithReceive)
        {
            if (!AllowSendsAtomicWithReceive(context.Settings))
            {
                throw new Exception(
                    $"The `{nameof(TransportTransactionMode.SendsAtomicWithReceive)}` mode of Outbox has not been enabled.");
            }

            //In the SendsAtomicWithReceive mode the component the outbox operations are marked as dispatched via a control
            //message processed by SetAsDispatchedBehavior
            context.Services.AddTransient<IOutboxSeam>(provider =>
                new OutboxSeam(provider.GetRequiredService<IOutboxStorage>(), false));

            context.Pipeline.Register("ForceBatchDispatchToBeNonIsolated", new ForceBatchDispatchToBeNonIsolatedBehavior(), "Makes sure that the outbox operations are enlisted in the receive transaction.");
            context.Pipeline.Register("SetAsDispatchedBehavior", sp => new SetAsDispatchedBehavior(sp.GetRequiredService<IOutboxStorage>()), "Marks the outbox record as dispatched after all messages have been sent out.");
            context.Pipeline.Register("SendSetAsDispatchedMessageBehavior", sp => new SendSetAsDispatchedMessageBehavior(context.LocalQueueAddress(), sp.GetRequiredService<ITransportAddressResolver>()), "Adds the SetAsDispatched to the outbox message batch");
        }
        else if (context.Settings.GetRequiredTransactionModeForReceives() == TransportTransactionMode.ReceiveOnly)
        {
            // ForceBatchDispatchToBeIsolatedBehavior set the dispatch consistency to isolated which instructs
            // the transport to not enlist the outgoing operation in the incoming message transaction. Unfortunately
            // this is not enough. We cannot allow the transport to operate in SendsWithAtomicReceive because a transport
            // might then only release the outgoing operations when the incoming transport transaction is committed meaning
            // the actual sends would happen after we have set the outbox record as dispatched and not as part of
            // TransportReceiveToPhysicalMessageConnector fork into the batched dispatched phase. Should acknowledging
            // the incoming operation fail and the message be retried we would already have cleared the outbox record's
            // transport operations leading to outgoing message loss.
            context.Pipeline.Register("ForceBatchDispatchToBeIsolated", new ForceBatchDispatchToBeIsolatedBehavior(), "Makes sure that we dispatch straight to the transport so that we can safely set the outbox record to dispatched once the dispatch pipeline returns.");

            // In the ReceiveOnly mode the SetAsDispatched operation is executed right after the messages
            // are dispatched to the transport to minimize the likelihood of duplicate dispatch and conserve space
            context.Services.AddTransient<IOutboxSeam>(provider =>
                new OutboxSeam(provider.GetRequiredService<IOutboxStorage>(), true));
        }
        else
        {
            throw new Exception(
                $"Outbox requires transport to be running in `{nameof(TransportTransactionMode.ReceiveOnly)}` or `{nameof(TransportTransactionMode.SendsAtomicWithReceive)}` mode. Use the `{nameof(TransportDefinition.TransportTransactionMode)}` property on the transport definition to specify the transaction mode.");
        }
    }

    internal const string TimeToKeepDeduplicationEntries = "Outbox.TimeToKeepDeduplicationEntries";
}
