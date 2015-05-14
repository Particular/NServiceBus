namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Extensibility;

    /// <summary>
    ///     Allows the users to control how the send is performed
    /// </summary>
    public class SendLocalOptions:ExtendableOptions
    {
        readonly DateTime? at;
        readonly TimeSpan? delay;
        internal Dictionary<string, object> Context = new Dictionary<string, object>();
    
        /// <summary>
        ///     Creates an instance of <see cref="SendOptions" />.
        /// </summary>
        /// <param name="deliverAt">Tells the bus to deliver the message at the given time.</param>
        /// <param name="delayDeliveryFor">Tells the bus to wait the specified amount of time before delivering the message.</param>
        public SendLocalOptions(DateTime? deliverAt = null, TimeSpan? delayDeliveryFor = null)
        {
            if (deliverAt != null && delayDeliveryFor != null)
            {
                throw new ArgumentException("Ensure you either set `deliverAt` or `delayDeliveryFor`, but not both.");
            }

            delay = delayDeliveryFor;
            at = deliverAt;
        }

        internal TimeSpan? Delay
        {
            get { return delay; }
        }

        internal DateTime? At
        {
            get { return at; }
        }
    }
}