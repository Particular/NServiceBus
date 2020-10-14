namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Logging;
    using Transport;

    class TransportReceiver
    {
        public TransportReceiver(
            string id,
            IPushMessages pushMessages,
            PushSettings pushSettings,
            PushRuntimeSettings pushRuntimeSettings)
        {
            Id = id;
            this.pushRuntimeSettings = pushRuntimeSettings;
            this.pushSettings = pushSettings;

            receiver = pushMessages;
        }

        //TODO add to IPushMessages
        public string Id { get; }

        public Task Start(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError)
        {
            if (isStarted)
            {
                throw new InvalidOperationException("The transport is already started");
            }

            Logger.DebugFormat("Receiver {0} is starting, listening to queue {1}.", Id, pushSettings.InputQueue);

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
                Logger.Warn($"Receiver {Id} listening to queue {pushSettings.InputQueue} threw an exception on stopping.", exception);
            }
            finally
            {
                isStarted = false;
            }
        }

        bool isStarted;
        PushRuntimeSettings pushRuntimeSettings;
        PushSettings pushSettings;

        //hack: make this accessible more easily for now so we can access the subscription storage
        internal IPushMessages receiver;

        static ILog Logger = LogManager.GetLogger<TransportReceiver>();
    }
}