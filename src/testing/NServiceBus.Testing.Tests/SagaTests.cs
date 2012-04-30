using System;
using NServiceBus.Saga;
using NUnit.Framework;

namespace NServiceBus.Testing.Tests
{
    [TestFixture]
    class SagaTests
    {
        [Test]
        public void MySaga()
        {
            Test.Saga<MySaga>()
                    .ExpectReplyToOrginator<ResponseToOriginator>()
                    .ExpectTimeoutToBeSetIn<StartsSaga>((state, span) => span == TimeSpan.FromDays(7))
                    .ExpectPublish<Event>()
                    .ExpectSend<Command>()
                .When(s => s.Handle(new StartsSaga()))
                    .ExpectPublish<Event>()
                .WhenSagaTimesOut()
                    .AssertSagaCompletionIs(true);
        }

        [Test]
        public void DiscountTest()
        {
            decimal total = 600;

            Test.Saga<DiscountPolicy>()
                    .ExpectSend<ProcessOrder>(m => m.Total == total)
                    .ExpectTimeoutToBeSetIn<SubmitOrder>((state, span) => span == TimeSpan.FromDays(7))
                .When(s => s.Handle(new SubmitOrder { Total = total }))
                    .ExpectSend<ProcessOrder>(m => m.Total == total * (decimal)0.9)
                    .ExpectTimeoutToBeSetIn<SubmitOrder>((state, span) => span == TimeSpan.FromDays(7))
                .When(s => s.Handle(new SubmitOrder { Total = total }));
        }

        [Test]
        public void DiscountTestWithTimeout()
        {
            decimal total = 600;

            Test.Saga<DiscountPolicy>()
                    .ExpectSend<ProcessOrder>(m => m.Total == total)
                    .ExpectTimeoutToBeSetIn<SubmitOrder>((state, span) => span == TimeSpan.FromDays(7))
                .When(s => s.Handle(new SubmitOrder { Total = total }))
                .WhenSagaTimesOut()
                    .ExpectSend<ProcessOrder>(m => m.Total == total)
                    .ExpectTimeoutToBeSetIn<SubmitOrder>((state, span) => span == TimeSpan.FromDays(7))
                .When(s => s.Handle(new SubmitOrder { Total = total }));
        }

        [Test]
        public void DiscountTestWithSpecificTimeout()
        {
            Test.Saga<DiscountPolicy>()
                    .ExpectSend<ProcessOrder>(m => m.Total == 500)
                    .ExpectTimeoutToBeSetIn<SubmitOrder>((state, span) => span == TimeSpan.FromDays(7))
                .When(s => s.Handle(new SubmitOrder { Total = 500 }))
                    .ExpectSend<ProcessOrder>(m => m.Total == 400)
                    .ExpectTimeoutToBeSetIn<SubmitOrder>((state, span) => span == TimeSpan.FromDays(7))
                .When(s => s.Handle(new SubmitOrder { Total = 400 }))
                    .ExpectSend<ProcessOrder>(m => m.Total == 300 * (decimal)0.9)
                    .ExpectTimeoutToBeSetIn<SubmitOrder>((state, span) => span == TimeSpan.FromDays(7))
                .When(s => s.Handle(new SubmitOrder { Total = 300 }))
                .WhenSagaTimesOut()
                    .ExpectSend<ProcessOrder>(m => m.Total == 200)
                    .ExpectTimeoutToBeSetIn<SubmitOrder>((state, span) => span == TimeSpan.FromDays(7))
                .When(s => s.Handle(new SubmitOrder { Total = 200 }));
        }


        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            Test.Initialize();
        }
    }

    public class MySaga : NServiceBus.Saga.Saga<MySagaData>,
        IAmStartedByMessages<StartsSaga>,
        IHandleTimeouts<StartsSaga>
    {
        public void Handle(StartsSaga message)
        {
            ReplyToOriginator<ResponseToOriginator>(m => { });
            Bus.Publish<Event>();
            Bus.Send<Command>(null);
            RequestUtcTimeout(TimeSpan.FromDays(7), message);
        }

        public void Timeout(StartsSaga state)
        {
            Bus.Publish<Event>();
            MarkAsComplete();
        }
    }

    public class MySagaData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }

    public class StartsSaga : ICommand {}
    public class ResponseToOriginator : IMessage {}
    public interface Event : IEvent { }
    public class Command : ICommand {}


    public class DiscountPolicy : Saga.Saga<DiscountPolicyData>,
    IAmStartedByMessages<SubmitOrder>,
        IHandleTimeouts<SubmitOrder>
    {
        public void Handle(SubmitOrder message)
        {
            Data.CustomerId = message.CustomerId;
            Data.RunningTotal += message.Total;

            if (Data.RunningTotal >= 1000)
                ProcessOrderWithDiscount(message);
            else
                ProcessOrder(message);

            RequestUtcTimeout(TimeSpan.FromDays(7), message);
        }

        public void Timeout(SubmitOrder state)
        {
            Data.RunningTotal -= state.Total;
        }

        private void ProcessOrder(SubmitOrder message)
        {
            Bus.Send<ProcessOrder>(m =>
            {
                m.CustomerId = Data.CustomerId;
                m.OrderId = message.OrderId;
                m.Total = message.Total;
            });
        }

        private void ProcessOrderWithDiscount(SubmitOrder message)
        {
            Bus.Send<ProcessOrder>(m =>
            {
                m.CustomerId = Data.CustomerId;
                m.OrderId = message.OrderId;
                m.Total = message.Total * (decimal)0.9;
            });
        }
    }

    public class SubmitOrder : IMessage
    {
        public Guid OrderId { get; set; }
        public Guid CustomerId { get; set; }
        public decimal Total { get; set; }
    }

    public class ProcessOrder : IMessage
    {
        public Guid OrderId { get; set; }
        public Guid CustomerId { get; set; }
        public decimal Total { get; set; }
    }

    public class DiscountPolicyData : ISagaEntity
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        public Guid CustomerId { get; set; }
        public decimal RunningTotal { get; set; }
    }

}
