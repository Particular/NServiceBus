namespace NServiceBus.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;

    /// <summary>
    /// Contains extension methods for saga specific operations.
    /// </summary>
    public static class MessageHandlerContextSagaExtensions
    {
        /// <summary>
        /// The saga's strongly typed data.
        /// </summary>
        /// <param name="context">The context to extend.</param>
        /// <typeparam name="TSagaData">The type of the saga's data.</typeparam>
        public static TSagaData GetSagaData<TSagaData>(this IMessageHandlerContext context)
            where TSagaData : class, IContainSagaData, new()
        {
            return context.GetSaga().Entity as TSagaData;
        }

        /// <summary>
        /// Request for a timeout to occur at the given <see cref="DateTime"/>.
        /// </summary>
        /// <param name="context">The context which is used to send the timeout.</param>
        /// <param name="at"><see cref="DateTime"/> to send timeout <typeparamref name="TTimeoutMessageType"/>.</param>
        public static Task RequestTimeoutAsync<TTimeoutMessageType>(this IMessageHandlerContext context, DateTime at) where TTimeoutMessageType : new()
        {
            return context.RequestTimeoutAsync(at, new TTimeoutMessageType());
        }

        /// <summary>
        /// Request for a timeout to occur at the given <see cref="DateTime"/>.
        /// </summary>
        /// <param name="context">The context which is used to send the timeout.</param>
        /// <param name="at"><see cref="DateTime"/> to send timeout <paramref name="timeoutMessage"/>.</param>
        /// <param name="timeoutMessage">The message to send after <paramref name="at"/> is reached.</param>
        public static Task RequestTimeoutAsync<TTimeoutMessageType>(this IMessageHandlerContext context, DateTime at, TTimeoutMessageType timeoutMessage)
        {
            if (at.Kind == DateTimeKind.Unspecified)
            {
                throw new InvalidOperationException("Kind property of DateTime 'at' must be specified.");
            }

            var saga = context.GetSaga();

            VerifySagaCanHandleTimeout(timeoutMessage, saga);

            var sendOptions = new SendOptions();

            sendOptions.DoNotDeliverBefore(at);
            sendOptions.RouteToLocalEndpointInstance();

            SetTimeoutHeaders(sendOptions, saga);

            return context.SendAsync(timeoutMessage, sendOptions);
        }

        /// <summary>
        /// Request for a timeout to occur within the give <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="context">The context which is used to send the timeout.</param>
        /// <param name="within">Given <see cref="TimeSpan"/> to delay timeout message by.</param>
        public static Task RequestTimeoutAsync<TTimeoutMessageType>(this IMessageHandlerContext context, TimeSpan within) where TTimeoutMessageType : new()
        {
            return context.RequestTimeoutAsync(within, new TTimeoutMessageType());
        }

        /// <summary>
        /// Request for a timeout to occur within the given <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="context">The context which is used to send the timeout.</param>
        /// <param name="within">Given <see cref="TimeSpan"/> to delay timeout message by.</param>
        /// <param name="timeoutMessage">The message to send after <paramref name="within"/> expires.</param>
        public static Task RequestTimeoutAsync<TTimeoutMessageType>(this IMessageHandlerContext context, TimeSpan within, TTimeoutMessageType timeoutMessage)
        {
            var saga = context.GetSaga();

            VerifySagaCanHandleTimeout(timeoutMessage, saga);

            var sendOptions = new SendOptions();

            sendOptions.DelayDeliveryWith(within);
            sendOptions.RouteToLocalEndpointInstance();

            SetTimeoutHeaders(sendOptions, saga);

            return context.SendAsync(timeoutMessage, sendOptions);
        }

        /// <summary>
        /// Sends the <paramref name="message"/> using the bus to the endpoint that caused this saga to start.
        /// </summary>
        public static Task ReplyToOriginatorAsync(this IMessageHandlerContext context, object message)
        {
            var saga = context.GetSaga();
            if (string.IsNullOrEmpty(saga.Entity.Originator))
            {
                throw new Exception("Entity.Originator cannot be null. Perhaps the sender is a SendOnly endpoint.");
            }

            var options = new ReplyOptions();

            options.OverrideReplyToAddressOfIncomingMessage(saga.Entity.Originator);
            options.SetCorrelationId(saga.Entity.OriginalMessageId);

            //until we have metadata we just set this to null to avoid our own saga id being set on outgoing messages since
            //that would cause the saga that started us (if it was a saga) to not be found. When we have metadata available in the future we'll set the correct id and type
            // and get true auto correlation to work between sagas
            options.Context.Set(new PopulateAutoCorrelationHeadersForRepliesBehavior.State
            {
                SagaTypeToUse = null,
                SagaIdToUse = null
            });

            return context.ReplyAsync(message, options);
        }

        /// <summary>
        /// Marks the saga as complete.
        /// This may result in the sagas state being deleted by the persister.
        /// </summary>
        public static void MarkAsComplete(this IMessageHandlerContext context)
        {
            context.GetSaga().MarkAsComplete();
        }

        static Saga GetSaga(this IMessageHandlerContext context)
        {
            var saga = context.Extensions.Get<ActiveSagaInstance>().Instance;
            return saga;
        }

        private static void SetTimeoutHeaders(ExtendableOptions options, Saga saga)
        {
            options.SetHeader(Headers.SagaId, saga.Entity.Id.ToString());
            options.SetHeader(Headers.IsSagaTimeoutMessage, bool.TrueString);
            options.SetHeader(Headers.SagaType, saga.GetType().AssemblyQualifiedName);
        }

        private static void VerifySagaCanHandleTimeout<TTimeoutMessageType>(TTimeoutMessageType timeoutMessage, Saga saga)
        {
            var canHandleTimeoutMessage = saga is IHandleTimeouts<TTimeoutMessageType>;
            if (!canHandleTimeoutMessage)
            {
                var message = $"The type '{saga.GetType().Name}' cannot request timeouts for '{timeoutMessage}' because it does not implement 'IHandleTimeouts<{typeof(TTimeoutMessageType).FullName}>'";
                throw new Exception(message);
            }
        }
    }
}