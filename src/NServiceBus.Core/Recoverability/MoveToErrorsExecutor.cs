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

            var headers = outgoingMessage.Headers;
            headers.Remove(Headers.DelayedRetries);
            headers.Remove(Headers.ImmediateRetries);

            ExceptionHeaderHelper.SetExceptionHeaders(headers, exception);

            foreach (var faultMetadata in staticFaultMetadata)
            {
                headers[faultMetadata.Key] = faultMetadata.Value;
            }

            headerCustomizations(headers);

            var transportOperations = new TransportOperations(new TransportOperation(outgoingMessage, new UnicastAddressTag(errorQueueAddress)));

            return dispatcher.Dispatch(transportOperations, transportTransaction, new ContextBag());
        }

        IDispatchMessages dispatcher;
        Dictionary<string, string> staticFaultMetadata;
        Action<Dictionary<string, string>> headerCustomizations;
    }
}