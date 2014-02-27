﻿namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using Pipeline;
    using Pipeline.Contexts;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class MultiSendValidatorBehavior : IBehavior<SendLogicalMessagesContext>
    {
        public void Invoke(SendLogicalMessagesContext context, Action next)
        {

            if (context.LogicalMessages.Count() > 1)
            {
                // Users can't send more than one message with a DataBusProperty in the same TransportMessage, Yes this is a limitation for now!
                var numberOfMessagesWithDataBusProperties = 0;
                foreach (var message in context.LogicalMessages)
                {
                    var hasAtLeastOneDataBusProperty = message.MessageType.GetProperties().Any(MessageConventionExtensions.IsDataBusProperty);

                    if (hasAtLeastOneDataBusProperty)
                    {
                        numberOfMessagesWithDataBusProperties++;
                    }
                }

                if (numberOfMessagesWithDataBusProperties > 1)
                {
                    throw new InvalidOperationException("This version of NServiceBus only supports sending up to one message with DataBusProperties per Send().");
                }
            }

            next();
        }
    }
}