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
        /// <param name="context"></param>
        /// <param name="next"></param>
        public override void Invoke(IncomingContext context, Action<TransportReceiveContext> next)
        {
            Invoke(context, x => next(new TransportReceiveContext(x, context)));
        }

        //TODO: change to header and body ony
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="onMessage"></param>
        protected abstract void Invoke(IncomingContext context, Action<TransportMessage> onMessage);

        /// <summary>
        /// 
        /// </summary>
        public class Registration : RegisterStep
        {
            /// <summary>
            /// 
            /// </summary>
            public Registration(): base("ReceiveMessage", typeof(ReceiveBehavior), "Try receive message from transport", false)
            {
            }
        }
    }

   
}