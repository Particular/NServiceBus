using System;
using Messages;
using NServiceBus;

namespace Server
{
    public class UpdateCustomerMessageHandler : BaseMessageHandler<UpdateCustomerMessage>
    {
        public override void Handle(UpdateCustomerMessage message)
        {
            //try uncommenting the following line to see the retry behavior
            //throw new Exception();

            Console.WriteLine("Trying to update customer {0}", message.CustomerId);
            if (new Random(DateTime.Now.Millisecond).Next(10) > 5)
            {
                this.Bus.Return((int)ErrorCode.None);
                
                CustomerUpdatedMessage toPublish = new CustomerUpdatedMessage();
                toPublish.CustomerId = message.CustomerId;

                this.Bus.Publish(toPublish);
                return;
            }

            this.Bus.Return((int)ErrorCode.NotFound);
        }
    }
}
