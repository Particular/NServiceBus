namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;

    /// <summary>
    /// Synchronous request/response extension methods.
    /// </summary>
    public static class RequestResponseExtensions
    {
        /// <summary>
        /// Sends a <paramref name="requestMessage"/> to the configured destination and returns back a <see cref="SendContext{TResponse}"/> that allows the use user to wait on.
        /// </summary>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="bus">Object beeing extended.</param>
        /// <param name="requestMessage">The request message.</param>
        /// <param name="options">The options for the send.</param>
        /// <returns>A synchronous request/response context.</returns>
        public static SendContext<TResponse> SynchronousRequestResponse<TResponse>(this IBus bus, object requestMessage, SynchronousOptions options)
        {
            if (requestMessage == null)
            {
                throw new ArgumentNullException("requestMessage");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (bus == null)
            {
                throw new ArgumentNullException("bus");
            }

            var sendOptions = new SendOptions(options.Destination, options.CorrelationId);
                
            foreach (var header in options.Headers)
            {
                sendOptions.AddHeader(header.Key, header.Value);
            }

            sendOptions
                .AddHeader("NServiceBus.HasCallback", Boolean.TrueString)
                .SetCustomMessageId(options.MessageId);

            var tcs = new TaskCompletionSource<TResponse>();

            sendOptions.GetContext().Set(new UpdateRequestResponseCorrelationTableBehavior.ExtensionState(tcs));

            bus.Send(requestMessage, sendOptions);

            return new SendContext<TResponse>(tcs);
        }

        /// <summary>
        /// Sends a <paramref name="requestMessage"/> to the configured destination and returns back a <see cref="SendContext{TResponse}"/> that allows the use user to wait on.
        /// </summary>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="bus">Object beeing extended.</param>
        /// <param name="requestMessage">The request message.</param>
        /// <param name="options">The options for the send.</param>
        /// <returns>A synchronous request/response context.</returns>
        public static SendContext<TResponse> SynchronousRequestResponse<TResponse>(this IBus bus, object requestMessage, SynchronousLocalOptions options)
        {
            if (requestMessage == null)
            {
                throw new ArgumentNullException("requestMessage");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (bus == null)
            {
                throw new ArgumentNullException("bus");
            }

            var sendLocalOptions = new SendLocalOptions(options.CorrelationId);

            foreach (var header in options.Headers)
            {
                sendLocalOptions.AddHeader(header.Key, header.Value);
            }

            sendLocalOptions
                .AddHeader("NServiceBus.HasCallback", Boolean.TrueString)
                .SetCustomMessageId(options.MessageId);

            var tcs = new TaskCompletionSource<TResponse>();

            sendLocalOptions.GetContext().Set(new UpdateRequestResponseCorrelationTableBehavior.ExtensionState(tcs));

            bus.SendLocal(requestMessage, sendLocalOptions);

            return new SendContext<TResponse>(tcs);
        }
    }
}
