namespace NServiceBus.Transports
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    /// <summary>
    /// 
    /// </summary>
    public abstract class ReceiveBehavior : StageConnector<IncomingContext, TransportReceiveContext>
    {
        /// <summary>
        /// 
        /// </summary>
        public override void Invoke(IncomingContext context, Action<TransportReceiveContext> next)
        {
            Invoke(context, x => next(new TransportReceiveContext(x, context)));
        }

        //TODO: change to header and body ony
        /// <summary>
        /// 
        /// </summary>
        protected abstract void Invoke(IncomingContext context, Action<IncomingMessage> onMessage);

        /// <summary>
        /// 
        /// </summary>
        public class Registration : RegisterStep
        {
            /// <summary>
            /// 
            /// </summary>
            public Registration(): base("ReceiveMessage", typeof(ReceiveBehavior), "Try receive message from transport")
            {
            }
        }
    }
}