namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using Routing;
    using Transport;

    class MoveToErrorsExecutor
    {
        public MoveToErrorsExecutor(IDispatchMessages dispatcher, string errorQueueAddress, Dictionary<string, string> staticFaultMetadata)
        {
            this.dispatcher = dispatcher;
            this.errorQueueAddress = errorQueueAddress;
            this.staticFaultMetadata = staticFaultMetadata;
        }

        public Task MoveToErrorQueue(IncomingMessage message, Exception exception, TransportTransaction transportTransaction)
        {
            message.RevertToOriginalBodyIfNeeded();

            var outgoingMessage = new OutgoingMessage(message.MessageId, new Dictionary<string, string>(message.Headers), message.Body);

            outgoingMessage.Headers.Remove(Headers.Retries);
            outgoingMessage.Headers.Remove(Headers.FLRetries);

            ExceptionHeaderHelper.SetExceptionHeaders(outgoingMessage.Headers, exception);

            foreach (var faultMetadata in staticFaultMetadata)
            {
                outgoingMessage.Headers[faultMetadata.Key] = faultMetadata.Value;
            }

            var transportOperations = new TransportOperations(new TransportOperation(outgoingMessage, new UnicastAddressTag(errorQueueAddress)));

            var dispatchContext = new ContextBag();
            dispatchContext.Set(transportTransaction);

            return dispatcher.Dispatch(transportOperations, dispatchContext);
        }

        IDispatchMessages dispatcher;
        string errorQueueAddress;
        Dictionary<string, string> staticFaultMetadata;
    }
}