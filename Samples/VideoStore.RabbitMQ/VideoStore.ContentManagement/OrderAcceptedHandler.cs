namespace VideoStore.ContentManagement
{
    using System;
    using System.Diagnostics;
    using VideoStore.Common;
    using VideoStore.Messages.RequestResponse;
    using VideoStore.Messages.Events;
    using NServiceBus;

    public class OrderAcceptedHandler : IHandleMessages<OrderAccepted>
    {
        

        public IBus Bus { get; set; }

        public void Handle(OrderAccepted message)
        {
            if (DebugFlagMutator.Debug)
            {
                Debugger.Break();
            }

            Console.WriteLine("Order # {0} has been accepted, Let's provision the download -- Sending ProvisionDownloadRequest to the VideoStore.Operations endpoint", message.OrderNumber);
            
            //send out a request (a event will be published when the response comes back)
            Bus.Send<ProvisionDownloadRequest>(r =>
            {
                r.ClientId = message.ClientId;
                r.OrderNumber = message.OrderNumber;
                r.VideoIds = message.VideoIds;
            });

        }
    }
}