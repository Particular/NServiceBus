namespace MyServer
{
    using MyMessages.Commands;
    using MyMessages.RequestResponse;
    using NServiceBus;

    public class MyCommandHandler : IHandleMessages<OrderCommand>
    {
        public IBus Bus { get; set; }

        public void Handle(OrderCommand message)
        {
            if ((message.OrderNumber % 2) == 0)
            {
                //Simulates a failure
                Bus.Return(OrderStatus.Failed);
                return;
            }
            //send out a request (a event will be published when the response comes back)
            Bus.Send<MyRequest>(r => r.RequestData = string.Format("Send a present to {0}", message.OrderNumber));

            //tell the client that we accepted the command
            Bus.Return(OrderStatus.Ok);
        }
    }
}