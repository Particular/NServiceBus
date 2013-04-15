using VideoStore.Messages.RequestResponse;

namespace VideoStore.DownloadVideos
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Common;
    using VideoStore.Messages.Events;
    using NServiceBus;

    public class OrderAcceptedHandler : IHandleMessages<OrderAccepted>
    {
        

        public IBus Bus { get; set; }

        public void Handle(OrderAccepted message)
        {
            Console.WriteLine("Order # {0} has been accepted, Let's provision the download -- Sending ProvisionDownloadReqiest to the VideoStore.Operations endpoint", message.OrderNumber);
            if (DebugFlagMutator.Debug)
            {
                Debugger.Break();
            }

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