namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Routing;
    using Transport;

    class MoveToErrorsExecutor
    {
        public MoveToErrorsExecutor(IMessageDispatcher dispatcher, Dictionary<string, string> staticFaultMetadata, Action<HeaderDictionary> headerCustomizations)
        {
            this.dispatcher = dispatcher;
            this.staticFaultMetadata = staticFaultMetadata;
            this.headerCustomizations = headerCustomizations;
        }

        public Task MoveToErrorQueue(string errorQueueAddress, IncomingMessage message, Exception exception, TransportTransaction transportTransaction, CancellationToken cancellationToken = default)
        {
            message.RevertToOriginalBodyIfNeeded();

            var outgoingMessage = new OutgoingMessage(message.MessageId, new HeaderDictionary(message.Headers), message.Body);

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

            return dispatcher.Dispatch(transportOperations, transportTransaction, cancellationToken);
        }

        IMessageDispatcher dispatcher;
        Dictionary<string, string> staticFaultMetadata;
        Action<HeaderDictionary> headerCustomizations;
    }
}