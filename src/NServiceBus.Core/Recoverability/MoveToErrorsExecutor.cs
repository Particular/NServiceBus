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
        public MoveToErrorsExecutor(IDispatchMessages dispatcher, Dictionary<string, string> staticFaultMetadata, Action<Dictionary<string, string>> headerCustomizations)
        {
            this.dispatcher = dispatcher;
            this.staticFaultMetadata = staticFaultMetadata;
            this.headerCustomizations = headerCustomizations;
        }

        public Task MoveToErrorQueue(string errorQueueAddress, IncomingMessage message, Exception exception, TransportTransaction transportTransaction)
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

            headerCustomizations(outgoingMessage.Headers);

            var transportOperations = new TransportOperations(new TransportOperation(outgoingMessage, new UnicastAddressTag(errorQueueAddress)));

            return dispatcher.Dispatch(transportOperations, transportTransaction, new ContextBag());
        }

        IDispatchMessages dispatcher;
        Dictionary<string, string> staticFaultMetadata;
        Action<Dictionary<string, string>> headerCustomizations;
    }
}