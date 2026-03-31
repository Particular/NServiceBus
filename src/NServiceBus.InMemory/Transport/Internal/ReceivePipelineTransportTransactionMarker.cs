namespace NServiceBus;

using Transport;

sealed class ReceivePipelineTransportTransactionMarker
{
    public static readonly ReceivePipelineTransportTransactionMarker Instance = new();

    ReceivePipelineTransportTransactionMarker() { }
}

static class TransportTransactionExtensions
{
    public static bool IsInsideReceivePipeline(this TransportTransaction transaction) =>
        transaction.TryGet<ReceivePipelineTransportTransactionMarker>(out _);
}