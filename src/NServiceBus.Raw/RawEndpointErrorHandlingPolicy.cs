using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Faults;
using NServiceBus.Routing;
using NServiceBus.Support;
using NServiceBus.Transport;

namespace NServiceBus.Raw
{
    using System;

    class RawEndpointErrorHandlingPolicy
    {
        string localAddress;
        IDispatchMessages dispatcher;
        Dictionary<string, string> staticFaultMetadata;
        IErrorHandlingPolicy policy;

        public RawEndpointErrorHandlingPolicy(string endpointName, string localAddress, IDispatchMessages dispatcher, IErrorHandlingPolicy policy)
        {
            this.localAddress = localAddress;
            this.dispatcher = dispatcher;
            this.policy = policy;

            staticFaultMetadata = new Dictionary<string, string>
            {
                {FaultsHeaderKeys.FailedQ, localAddress},
                {Headers.ProcessingMachine, RuntimeEnvironment.MachineName},
                {Headers.ProcessingEndpoint, endpointName},
            };
        }

        public Task<ErrorHandleResult> OnError(ErrorContext errorContext)
        {
            return policy.OnError(new Context(localAddress, errorContext, MoveToErrorQueue), dispatcher);
        }

        async Task<ErrorHandleResult> MoveToErrorQueue(ErrorContext errorContext, string errorQueue, bool includeStandardHeaders)
        {
            var message = errorContext.Message;

            var outgoingMessage = new OutgoingMessage(message.MessageId, new Dictionary<string, string>(message.Headers), message.Body);

            var headers = outgoingMessage.Headers;
            headers.Remove(Headers.DelayedRetries);
            headers.Remove(Headers.ImmediateRetries);

            ExceptionHeaderHelper.SetExceptionHeaders(headers, errorContext.Exception);

            if (includeStandardHeaders)
            {
                foreach (var faultMetadata in staticFaultMetadata)
                {
                    headers[faultMetadata.Key] = faultMetadata.Value;
                }
            }
            var transportOperations = new TransportOperations(new TransportOperation(outgoingMessage, new UnicastAddressTag(errorQueue)));

            await dispatcher.Dispatch(transportOperations, errorContext.TransportTransaction, new ContextBag()).ConfigureAwait(false);
            return ErrorHandleResult.Handled;
        }

        class Context : IErrorHandlingPolicyContext
        {
            Func<ErrorContext, string, bool, Task<ErrorHandleResult>> moveToErrorQueue;

            public Context(string failedQueue, ErrorContext error, Func<ErrorContext, string, bool, Task<ErrorHandleResult>> moveToErrorQueue)
            {
                this.moveToErrorQueue = moveToErrorQueue;
                Error = error;
                FailedQueue = failedQueue;
            }

            public Task<ErrorHandleResult> MoveToErrorQueue(string errorQueue, bool attachStandardFailureHeaders = true)
            {
                return moveToErrorQueue(Error, errorQueue, attachStandardFailureHeaders);
            }

            public ErrorContext Error { get; }
            public string FailedQueue { get; }
        }
    }
}