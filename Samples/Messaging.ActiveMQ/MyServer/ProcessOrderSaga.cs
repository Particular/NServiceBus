namespace MyServer
{
    using System;
    using MyMessages.Commands;
    using MyMessages.Events;
    using MyMessages.RequestResponse;
    using NServiceBus;
    using NServiceBus.Saga;

    public class ProcessOrderSaga : Saga<OrderData>,
                                    ISagaStartedBy<InventoryResponse>,
                                    IHandleMessages<CancelOrder>,
                                    IHandleTimeouts<CoolDownPeriod>
    {
        public void Handle(InventoryResponse message)
        {
            Data.OrderNumber = message.OrderNumber;
            Data.VideoIds = message.VideoIds;

            RequestTimeout(TimeSpan.FromSeconds(30), new CoolDownPeriod());
            Console.Out.WriteLine("Starting cool down period for order #{0}.", Data.OrderNumber);
        }

        public void Timeout(CoolDownPeriod state)
        {
            Bus.Publish<CoolDownPeriodElapsed>(e =>
                {
                    e.OrderNumber = Data.OrderNumber;
                    e.VideoIds = Data.VideoIds;
                });

            MarkAsComplete();

            Console.Out.WriteLine("Cooling down period for order #{0} has elapsed.", Data.OrderNumber);
        }

        public void Handle(CancelOrder message)
        {
            MarkAsComplete();
            Bus.Return(OrderStatus.Ok);
            Console.Out.WriteLine("Order #{0} was cancelled.", message.OrderNumber);
        }

        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<InventoryResponse>(s => s.OrderNumber, m => m.OrderNumber);
            ConfigureMapping<CancelOrder>(s => s.OrderNumber, m => m.OrderNumber);
        }
    }

    public class OrderData : ISagaEntity
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        [Unique]
        public int OrderNumber { get; set; }

        public string[] VideoIds { get; set; }
    }

    public class CoolDownPeriod
    {
    }
}