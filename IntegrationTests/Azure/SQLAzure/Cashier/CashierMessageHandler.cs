using System;
using Cashier.ViewData;
using CashierContracts;
using CustomerContracts;
using NServiceBus;
using NServiceBus.Saga;

namespace Cashier
{
    public class CashierSaga : Saga<CashierSagaData>,
                                IAmStartedByMessages<NewOrderMessage>,
                                IHandleMessages<PaymentMessage>
    {
        private readonly IStarbucksCashierView _view;

        public CashierSaga()
        {}

        public CashierSaga(IStarbucksCashierView view)
        {
            _view = view;
        }

        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<NewOrderMessage>(s => s.OrderId).ToSaga(m => m.OrderId);
            ConfigureMapping<PaymentMessage>(s => s.OrderId).ToSaga(m => m.OrderId);
        }
        
        public void Handle(NewOrderMessage message)
        {
            _view.NewOrder(new NewOrderView(message));

            Data.Drink = message.Drink;
            Data.DrinkSize = message.DrinkSize;
            Data.OrderId = message.OrderId;
            Data.CustomerName = message.CustomerName;
            Data.Amount = CalculateAmountAccordingTo(message.DrinkSize);

            Bus.Send(new PrepareOrderMessage(Data.CustomerName, Data.Drink, Data.DrinkSize, Data.OrderId));
            Bus.Reply(new PaymentRequestMessage(Data.Amount, message.CustomerName, message.OrderId));
        }

        public void Handle(PaymentMessage message)
        {
            if(message.Amount >= Data.Amount)
            {
                var viewData = new ReceivedFullPaymentView(Data.CustomerName, Data.Drink, Data.DrinkSize);
                _view.ReceivedFullPayment(viewData);

                Bus.Publish(new PaymentCompleteMessage(Data.OrderId));
            }
            else if(message.Amount == 0)
            {
                var viewData = new CustomerRefusesToPayView(Data.CustomerName, Data.Amount, Data.Drink, Data.DrinkSize);
                _view.CustomerRefusesToPay(viewData);
            }

            MarkAsComplete();
        }

        private static Double CalculateAmountAccordingTo(DrinkSize size)
        {
            switch(size)
            {
                case DrinkSize.Tall:
                    return 3.25;
                case DrinkSize.Grande:
                    return 4.00;
                case DrinkSize.Venti:
                    return 4.75;
                default:
                    throw new InvalidOperationException(String.Format("Size '{0}' does not compute!", size));
            }
        }
    }
}
