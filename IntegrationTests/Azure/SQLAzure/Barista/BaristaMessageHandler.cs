using System;
using System.Threading;
using Barista.ViewData;
using CashierContracts;
using CustomerContracts;
using NServiceBus;
using NServiceBus.Saga;

namespace Barista
{
    public class BaristaMessageHandler : Saga<BaristaSagaData>,
                                         IAmStartedByMessages<PrepareOrderMessage>,
                                         IHandleMessages<PaymentCompleteMessage>,
        IHandleTimeouts<CleanUpOrders>
    {
        private readonly IStarbucksBaristaView _view;

        public BaristaMessageHandler()
        {}

        public BaristaMessageHandler(IStarbucksBaristaView view)
        {
            _view = view;
        }

        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<PrepareOrderMessage>(s => s.OrderId).ToSaga(m => m.OrderId);
            ConfigureMapping<PaymentCompleteMessage>(s => s.OrderId).ToSaga(m => m.OrderId);
        }

        public void Handle(PrepareOrderMessage message)
        {
            var viewData = new PrepareOrderView(message.CustomerName, message.Drink, message.DrinkSize);
            _view.PrepareOrder(viewData);

            Data.CustomerName = message.CustomerName;
            Data.Drink = message.Drink;
            Data.OrderId = message.OrderId;
            Data.Size = message.DrinkSize;

            RequestTimeout<CleanUpOrders>(TimeSpan.FromMinutes(1));
                        
            for(var i=0; i<10; i++)
            {
                Thread.Sleep(1000);
            }

            var additionalViewData = new OrderIsDoneView(message.CustomerName);
            _view.OrderIsDone(additionalViewData);

            Data.OrderIsReady = true;
            DeliverOrder();
        }

        public void Handle(PaymentCompleteMessage message)
        {
            Data.OrderIsPaid = true;
            DeliverOrder();
        }

        private void DeliverOrder()
        {
            if (!Data.OrderIsReady || !Data.OrderIsPaid)
                return;

            var viewData = new DeliverOrderView(Data.Drink, Data.Size);
            _view.DeliverOrder(viewData);

            Bus.Publish(new OrderReadyMessage(Data.Drink,Data.CustomerName));

            MarkAsComplete();
        }

        public void Timeout(CleanUpOrders state)
        {
            if (!Data.OrderIsReady || !Data.OrderIsPaid)
            {
                var viewData = new OrderIsTrashedView(Data.Drink, Data.CustomerName, Data.Size);
                _view.TrashOrder(viewData);
                MarkAsComplete();
            }
            else
            {
                DeliverOrder();
            }
        }
    }

    public class CleanUpOrders
    {
    }
}
