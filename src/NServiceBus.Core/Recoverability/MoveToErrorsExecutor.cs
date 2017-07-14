namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Routing;
    using Transport;
    using Pipeline;

    class MoveToErrorsExecutor
    {
        public MoveToErrorsExecutor(Dictionary<string, string> staticFaultMetadata, Action<Dictionary<string, string>> headerCustomizations)
        {
            this.staticFaultMetadata = staticFaultMetadata;
            this.headerCustomizations = headerCustomizations;
        }

        public Task MoveToErrorQueue(string errorQueueAddress, IncomingMessage message, ErrorContext context)
        {
            message.RevertToOriginalBodyIfNeeded();

            var outgoingMessage = new OutgoingMessage(message.MessageId, new Dictionary<string, string>(message.Headers), message.Body);

            var headers = outgoingMessage.Headers;
            headers.Remove(Headers.DelayedRetries);
            headers.Remove(Headers.ImmediateRetries);

            ExceptionHeaderHelper.SetExceptionHeaders(headers, context.Exception);

            foreach (var faultMetadata in staticFaultMetadata)
            {
                headers[faultMetadata.Key] = faultMetadata.Value;
            }

            headerCustomizations(headers);

            return context.Dispatch(new TransportOperation(outgoingMessage, new UnicastAddressTag(errorQueueAddress)));
        }

        Dictionary<string, string> staticFaultMetadata;
        Action<Dictionary<string, string>> headerCustomizations;
    }
}