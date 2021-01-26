namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Transport;

    class TransportReceiver
    {
        public TransportReceiver(
            IMessageReceiver receiver,
            PushRuntimeSettings pushRuntimeSettings)
        {
            this.pushRuntimeSettings = pushRuntimeSettings;
            this.receiver = receiver;
        }

        public async Task Start(OnMessage onMessage, OnError onError, CancellationToken cancellationToken)
        {
            if (isStarted)
            {
                throw new InvalidOperationException("The transport is already started");
            }

            Logger.DebugFormat("Receiver {0} is starting.", receiver.Id);


            await receiver.Initialize(pushRuntimeSettings, onMessage, onError, cancellationToken).ConfigureAwait(false);
            await receiver.StartReceive(cancellationToken).ConfigureAwait(false);

            isStarted = true;
        }

        public async Task Stop(CancellationToken cancellationToken)
        {
            if (!isStarted)
            {
                return;
            }

            try
            {
                await receiver.StopReceive(cancellationToken).ConfigureAwait(false);
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
        internal IMessageReceiver receiver;

        static ILog Logger = LogManager.GetLogger<TransportReceiver>();
    }
}