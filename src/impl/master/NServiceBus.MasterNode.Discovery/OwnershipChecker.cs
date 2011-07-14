using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus.MessageMutator;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.MasterNode.Discovery
{
    class OwnershipChecker : IMutateTransportMessages
    {
        public void MutateIncoming(TransportMessage transportMessage)
        {
            //intentionally do nothing
        }

        public void MutateOutgoing(IMessage[] messages, TransportMessage transportMessage)
        {
            if (messages.Length == 0)
                return;

            if (transportMessage.MessageIntent == MessageIntentEnum.Publish)
            {
                foreach (var t in Bootstrapper.MessageTypesOwned)
                    if (t.IsAssignableFrom(messages[0].GetType()))
                        return;

                throw new InvalidOperationException("You are publishing a message of the type " +
                                                    messages[0].GetType().FullName +
                                                    " but have not implemented IAmResponsibleForMessages<T> for it.");
            }
        }
    }
}
