namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;

    /// <summary>
    ///  Request/response extension methods.
    /// </summary>
    public static class RequestResponseExtensions
    {
        /// <summary>
        /// Sends a <paramref name="requestMessage"/> to the configured destination and returns back a <see cref="Task{TResponse}"/> which can be awaited.
        /// </summary>
        /// <remarks> The task returned is non durable. When the AppDomain is unloaded or the response task is canceled. 
        /// Messages can still arrive to the requesting endpoint but in that case no handling code will be attached to consume
        ///  that response message and therefore the message will be moved to the error queue.</remarks>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="bus">Object beeing extended.</param>
        /// <param name="requestMessage">The request message.</param>
        /// <param name="options">The options for the send.</param>
        /// <returns>A task which contains the response when it is completed.</returns>
        public static Task<TResponse> RequestWithTransientlyHandledResponseAsync<TResponse>(this IBus bus, object requestMessage, RequestResponseOptions options)
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
                .AddHeader("$Routing.RouteReplyToSpecificEndpointInstance", Boolean.TrueString)
                .SetCustomMessageId(options.MessageId);

            var tcs = new TaskCompletionSource<TResponse>();

            sendOptions.GetContext().Set(new RequestResponse.State(tcs, options.CancellationToken));

            bus.Send(requestMessage, sendOptions);

            return tcs.Task;
        }

        /// <summary>
        /// Sends a <paramref name="requestMessage"/> to the configured destination and returns back a <see cref="Task{TResponse}"/> which can be awaited.
        /// </summary>
        /// <remarks> The task returned is non durable. When the AppDomain is unloaded or the response task is canceled. 
        /// Messages can still arrive to the requesting endpoint but in that case no handling code will be attached to consume
        ///  that response message and therefore the message will be moved to the error queue.</remarks>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="bus">Object beeing extended.</param>
        /// <param name="requestMessage">The request message.</param>
        /// <param name="options">The options for the send.</param>
        /// <returns>A task which contains the response when it is completed.</returns>
        public static Task<TResponse> RequestWithTransientlyHandledResponseAsync<TResponse>(this IBus bus, object requestMessage, RequestResponseLocalOptions options)
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
                .AddHeader("$Routing.RouteReplyToSpecificEndpointInstance", Boolean.TrueString)
                .SetCustomMessageId(options.MessageId);

            var tcs = new TaskCompletionSource<TResponse>();

            sendLocalOptions.GetContext().Set(new RequestResponse.State(tcs, options.CancellationToken));

            bus.SendLocal(requestMessage, sendLocalOptions);

            return tcs.Task;
        }
    }
}
