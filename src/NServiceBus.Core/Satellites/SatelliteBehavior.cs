namespace NServiceBus
{
    using System;

    /// <summary>
    /// 
    /// </summary>
    public abstract class SatelliteBehavior: PhysicalMessageProcessingStageBehavior
    {
        /// <summary>
        /// 
        /// </summary>
        public override void Invoke(Context context, Action next)
        {
            context.Set("TransportReceiver.MessageHandledSuccessfully", Handle(context.PhysicalMessage));
        }

        /// <summary>
        /// 
        /// </summary>
        protected abstract bool Handle(TransportMessage physicalMessage);
    }
}