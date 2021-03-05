namespace NServiceBus.TransportTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Logging;
    using NUnit.Framework;

    public class When_on_error_throws : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_invoke_critical_error_and_retry(TransportTransactionMode transactionMode)
        {
            var criticalErrorCalled = false;
            string criticalErrorMessage = null;
            string nativeMessageId = null;
            Exception criticalErrorException = null;
            var exceptionFromOnError = new Exception("Exception from onError");

            var retried = new TaskCompletionSource();
            OnTestTimeout(() => retried.SetCanceled());

            var retrying = false;

            await StartPump(
                (context, _) =>
                {
                    if (retrying)
                    {
                        return retried.SetCompleted();
                    }

                    nativeMessageId = context.NativeMessageId;

                    throw new Exception("Exception from onMessage");
                },
                (_, __) =>
                {
                    retrying = true;
                    throw exceptionFromOnError;
                },
                transactionMode,
                (message, exception, _) =>
                {
                    criticalErrorCalled = true;
                    criticalErrorMessage = message;
                    criticalErrorException = exception;
                });

            LogFactory.LogItems.Clear();

            await SendMessage(InputQueueName);

            await retried.Task;

            Assert.True(criticalErrorCalled, "Should invoke critical error");
            Assert.AreEqual($"Failed to execute recoverability policy for message with native ID: `{nativeMessageId}`", criticalErrorMessage);
            Assert.AreEqual(exceptionFromOnError, criticalErrorException);

            var logItemsAboveInfo = LogFactory.LogItems.Where(item => item.Level > LogLevel.Info).Select(log => $"{log.Level}: {log.Message}").ToArray();
            Assert.AreEqual(0, logItemsAboveInfo.Length, "Transport should not log anything above LogLevel.Info:" + string.Join(Environment.NewLine, logItemsAboveInfo));
        }
    }
}
