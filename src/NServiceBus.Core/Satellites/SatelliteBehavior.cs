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
        /// <param name="context"></param>
        /// <param name="next"></param>
        public override void Invoke(Context context, Action next)
        {
            context.Set("TransportReceiver.MessageHandledSuccessfully", Handle(context.PhysicalMessage));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="physicalMessage"></param>
        /// <returns></returns>
        protected abstract bool Handle(TransportMessage physicalMessage);
    }
}