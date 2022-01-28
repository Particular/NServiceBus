namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Faults;
    using Routing;
    using Transport;

    class MoveToErrorsExecutor
    {
        public MoveToErrorsExecutor(
            Dictionary<string, string> staticFaultMetadata,
            Action<Dictionary<string, string>> headerCustomizations)
        {
            this.staticFaultMetadata = staticFaultMetadata;
            this.headerCustomizations = headerCustomizations;
        }

        public Task MoveToErrorQueue(
            string errorQueueAddress,
            ErrorContext errorContext,
            Func<TransportOperation, CancellationToken, Task> dispatchAction,
            CancellationToken cancellationToken = default)
        {
            var message = errorContext.Message;
            var outgoingMessage = new OutgoingMessage(message.MessageId, new Dictionary<string, string>(message.Headers), message.Body);

            var headers = outgoingMessage.Headers;
            headers.Remove(Headers.DelayedRetries);
            headers.Remove(Headers.ImmediateRetries);

            headers[FaultsHeaderKeys.FailedQ] = errorContext.ReceiveAddress;

            ExceptionHeaderHelper.SetExceptionHeaders(headers, errorContext.Exception);

            foreach (var faultMetadata in staticFaultMetadata)
            {
                headers[faultMetadata.Key] = faultMetadata.Value;
            }

            headerCustomizations(headers);

            return dispatchAction(new TransportOperation(outgoingMessage, new UnicastAddressTag(errorQueueAddress)), cancellationToken);
        }

        readonly Dictionary<string, string> staticFaultMetadata;
        readonly Action<Dictionary<string, string>> headerCustomizations;
    }
}