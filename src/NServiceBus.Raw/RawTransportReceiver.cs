using System;
using System.Threading.Tasks;
using NServiceBus.Logging;
using NServiceBus.Transport;

namespace NServiceBus.Raw
{
    class RawTransportReceiver
    {
        public RawTransportReceiver(IPushMessages pushMessages, IDispatchMessages dispatcher, Func<MessageContext, IDispatchMessages, Task> onMessage, PushSettings pushSettings, PushRuntimeSettings pushRuntimeSettings, CriticalError criticalError, RawEndpointErrorHandlingPolicy errorHandlingPolicy)
        {
            this.criticalError = criticalError;
            this.errorHandlingPolicy = errorHandlingPolicy;
            this.pushRuntimeSettings = pushRuntimeSettings;
            this.pushSettings = pushSettings;

            receiver = pushMessages;
            this.onMessage = context => onMessage(context, dispatcher);
        }

        public Task Init()
        {
            return receiver.Init(ctx => onMessage(ctx), ctx => errorHandlingPolicy.OnError(ctx), criticalError, pushSettings);
        }

        public void Start()
        {
            if (isStarted)
            {
                throw new InvalidOperationException("The transport is already started");
            }

            Logger.DebugFormat("Receiver is starting, listening to queue {0}.", pushSettings.InputQueue);

            receiver.Start(pushRuntimeSettings);

            isStarted = true;
        }

        public async Task Stop()
        {
            if (!isStarted)
            {
                return;
            }

            await receiver.Stop().ConfigureAwait(false);

            isStarted = false;
        }

        CriticalError criticalError;
        RawEndpointErrorHandlingPolicy errorHandlingPolicy;
        bool isStarted;
        PushRuntimeSettings pushRuntimeSettings;
        PushSettings pushSettings;
        IPushMessages receiver;
        Func<MessageContext, Task> onMessage;

        static ILog Logger = LogManager.GetLogger<RawTransportReceiver>();
    }
}