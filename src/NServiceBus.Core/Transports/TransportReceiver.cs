namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Logging;
    using Transport;

    class TransportReceiver
    {
        public TransportReceiver(
            IPushMessages receiver,
            PushRuntimeSettings pushRuntimeSettings)
        {
            this.pushRuntimeSettings = pushRuntimeSettings;
            this.receiver = receiver;
        }

        public Task Start(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError)
        {
            if (isStarted)
            {
                throw new InvalidOperationException("The transport is already started");
            }

            Logger.DebugFormat("Receiver {0} is starting.", receiver.Id);

            receiver.Start(pushRuntimeSettings, onMessage, onError);

            isStarted = true;

            return Task.FromResult(0);
        }

        public async Task Stop()
        {
            if (!isStarted)
            {
                return;
            }

            try
            {
                await receiver.Stop().ConfigureAwait(false);
                (receiver as IDisposable)?.Dispose();
            }
            catch (Exception exception)
            {
                Logger.Warn($"Receiver {receiver.Id} threw an exception on stopping.", exception);
            }
            finally
            {
                isStarted = false;
            }
        }

        bool isStarted;
        PushRuntimeSettings pushRuntimeSettings;

        //hack: make this accessible more easily for now so we can access the subscription storage
        internal IPushMessages receiver;

        static ILog Logger = LogManager.GetLogger<TransportReceiver>();
    }
}