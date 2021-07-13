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
        public MoveToErrorsExecutor(IMessageDispatcher dispatcher, Dictionary<string, string> staticFaultMetadata, Action<Dictionary<string, string>> headerCustomizations)
        {
            this.dispatcher = dispatcher;
            this.staticFaultMetadata = staticFaultMetadata;
            this.headerCustomizations = headerCustomizations;
        }

        public async Task MoveToErrorQueue(string errorQueueAddress, IncomingMessage message, Exception exception, TransportTransaction transportTransaction, CancellationToken cancellationToken = default)
        {
            var headers = MainPipelineExecutor.HeaderPool.Get();
            message.Headers.CopyTo(headers);

            try
            {
                var outgoingMessage = new OutgoingMessage(message.MessageId, headers, message.Body);

                headers.Remove(Headers.DelayedRetries);
                headers.Remove(Headers.ImmediateRetries);

                ExceptionHeaderHelper.SetExceptionHeaders(headers, exception);

                foreach (var faultMetadata in staticFaultMetadata)
                {
                    headers[faultMetadata.Key] = faultMetadata.Value;
                }

                headerCustomizations(headers);

                var transportOperations =
                    new TransportOperations(new TransportOperation(outgoingMessage,
                        new UnicastAddressTag(errorQueueAddress)));

                await dispatcher.Dispatch(transportOperations, transportTransaction, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                MainPipelineExecutor.HeaderPool.Return(headers);
            }
        }

        IMessageDispatcher dispatcher;
        Dictionary<string, string> staticFaultMetadata;
        Action<Dictionary<string, string>> headerCustomizations;
    }
}