namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_on_error_throws : NServiceBusTransportTest
    {
        // [TestCase(TransportTransactionMode.None)] -- not relevant
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_reinvoke_on_error_with_original_exception(TransportTransactionMode transactionMode)
        {
            var onErrorCalled = new TaskCompletionSource<ErrorContext>();

            OnTestTimeout(() => onErrorCalled.SetCanceled());

            var firstInvocation = true;

            await StartPump(
                context =>
                {
                    throw new Exception("Simulated exception");
                },
                context =>
                {
                    if (firstInvocation)
                    {
                        firstInvocation = false;

                        throw new Exception("Exception from onError");
                    }

                    onErrorCalled.SetResult(context);

                    return Task.FromResult(ErrorHandleResult.Handled);
                },
                transactionMode);

            await SendMessage(InputQueueName);

            var errorContext = await onErrorCalled.Task;

            Assert.AreEqual("Simulated exception", errorContext.Exception.Message); 
       }
    }
}