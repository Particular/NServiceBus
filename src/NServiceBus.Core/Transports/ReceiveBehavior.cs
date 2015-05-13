namespace NServiceBus.Transports
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    /// <summary>
    /// Connects a <see cref="IncomingContext"/> with a <see cref="TransportReceiveContext"/>.
    /// </summary>
    public abstract class ReceiveBehavior : StageConnector<IncomingContext, TransportReceiveContext>
    {
        /// <inheritdoc />
        public override Task Invoke(IncomingContext context, Func<TransportReceiveContext, Task> next)
        {
            return Invoke(context, x => next(new TransportReceiveContext(x, context)));
        }

        //TODO: change to header and body ony
        /// <inheritdoc />
        protected abstract Task Invoke(IncomingContext context, Func<IncomingMessage, Task> onMessage);

        /// <summary>
        /// The <see cref="RegisterStep"/> for <see cref="ReceiveBehavior"/>.
        /// </summary>
        public class Registration : RegisterStep
        {
            /// <summary>
            /// Initializes a new insatnce of <see cref="ReceiveBehavior"/>.
            /// </summary>
            public Registration(): base("ReceiveMessage", typeof(ReceiveBehavior), "Try receive message from transport")
            {
            }
        }
    }
}