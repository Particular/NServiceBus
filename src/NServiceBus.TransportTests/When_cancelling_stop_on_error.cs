namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_cancelling_stop_on_error : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_cancel(TransportTransactionMode transactionMode)
        {
            var started = CreateTaskCompletionSource();
            var wasCanceled = CreateTaskCompletionSource<bool>();

            await StartPump(
                (_, __) => throw new Exception(),
                async (_, cancellationToken) =>
                {
                    started.SetResult();

                    try
                    {
                        await Task.Delay(TestTimeout, cancellationToken);
                    }
                    catch (Exception ex) when (ex.IsCausedBy(cancellationToken))
                    {
                        wasCanceled.SetResult(true);
                        throw;
                    }

                    wasCanceled.SetResult(false);

                    return ErrorHandleResult.Handled;
                },
                transactionMode);

            await SendMessage(InputQueueName);

            await started.Task;

            await StopPump(new CancellationToken(true));

            Assert.True(await wasCanceled.Task, "onError was not canceled.");
        }
    }
}
