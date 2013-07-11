using System;
using NServiceBus.Saga;
using NUnit.Framework;

namespace NServiceBus.Testing.Tests
{
    [TestFixture]
    internal class SagaTests
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            Test.Initialize();
        }

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
        public void MySagaWithActions()
        {
            Test.Saga<MySaga>()
                .ExpectTimeoutToBeSetIn<StartsSaga>(
                    (state, span) => Assert.That(() => span, Is.EqualTo(TimeSpan.FromDays(7))))
                .When(s => s.Handle(new StartsSaga()));
        }

        [Test]
        public void SagaThatDoesAReply()
        {
            Test.Saga<SagaThatDoesAReply>()
                .ExpectReply<MyReply>(reply => reply != null)
                .When(s => s.Handle(new MyRequest()));
        }

        [Test]
        public void DiscountTest()
        {
            decimal total = 600;

            Test.Saga<DiscountPolicy>()
                .ExpectSend<ProcessOrder>(m => m.Total == total)
                .ExpectTimeoutToBeSetIn<SubmitOrder>((state, span) => span == TimeSpan.FromDays(7))
                .When(s => s.Handle(new SubmitOrder {Total = total}))
                .ExpectSend<ProcessOrder>(m => m.Total == total*(decimal) 0.9)
                .ExpectTimeoutToBeSetIn<SubmitOrder>((state, span) => span == TimeSpan.FromDays(7))
                .When(s => s.Handle(new SubmitOrder {Total = total}));
        }

        [Test]
        public void DiscountTestWithActions()
        {
            decimal total = 600;

            Test.Saga<DiscountPolicy>()
                .ExpectSend<ProcessOrder>(m => Assert.That(() => m.Total, Is.EqualTo(total)))
                .When(s => s.Handle(new SubmitOrder { Total = total }));
        }

        [Test]
        public void DiscountTestWithTimeout()
        {
            decimal total = 600;

            Test.Saga<DiscountPolicy>()
                .ExpectSend<ProcessOrder>(m => m.Total == total)
                .ExpectTimeoutToBeSetIn<SubmitOrder>((state, span) => span == TimeSpan.FromDays(7))
                .When(s => s.Handle(new SubmitOrder {Total = total}))
                .WhenSagaTimesOut()
                .ExpectSend<ProcessOrder>(m => m.Total == total)
                .ExpectTimeoutToBeSetIn<SubmitOrder>((state, span) => span == TimeSpan.FromDays(7))
                .When(s => s.Handle(new SubmitOrder {Total = total}));
        }


        [Test]
        public void RemoteOrder()
        {
            decimal total = 100;

            Test.Saga<DiscountPolicy>()
                .ExpectSendToDestination<ProcessOrder>((m, a) => m.Total == total && a.Queue == "remote.orderqueue")
                .ExpectTimeoutToBeSetIn<SubmitOrder>((state, span) => span == TimeSpan.FromDays(7))
                .When(s => s.Handle(new SubmitOrder {Total = total, IsRemoteOrder = true}));
        }

        [Test]
        public void RemoteOrderWithAssertions()
        {
            decimal total = 100;

            Test.Saga<DiscountPolicy>()
                .ExpectSendToDestination<ProcessOrder>((m, a) => 
                {
                    Assert.That(() => m.Total, Is.EqualTo(total));
                    Assert.That(() => a.Queue, Is.EqualTo("remote.orderqueue"));
                })
                .ExpectTimeoutToBeSetIn<SubmitOrder>((state, span) => span == TimeSpan.FromDays(7))
                .When(s => s.Handle(new SubmitOrder { Total = total, IsRemoteOrder = true }));
        }


        [Test]
        public void DiscountTestWithSpecificTimeout()
        {
            Test.Saga<DiscountPolicy>()
                .ExpectSend<ProcessOrder>(m => m.Total == 500)
                .ExpectTimeoutToBeSetIn<SubmitOrder>((state, span) => span == TimeSpan.FromDays(7))
                .When(s => s.Handle(new SubmitOrder {Total = 500}))
                .ExpectSend<ProcessOrder>(m => m.Total == 400)
                .ExpectTimeoutToBeSetIn<SubmitOrder>((state, span) => span == TimeSpan.FromDays(7))
                .When(s => s.Handle(new SubmitOrder {Total = 400}))
                .ExpectSend<ProcessOrder>(m => m.Total == 300*(decimal) 0.9)
                .ExpectTimeoutToBeSetIn<SubmitOrder>((state, span) => span == TimeSpan.FromDays(7))
                .When(s => s.Handle(new SubmitOrder {Total = 300}))
                .WhenSagaTimesOut()
                .ExpectSend<ProcessOrder>(m => m.Total == 200)
                .ExpectTimeoutToBeSetIn<SubmitOrder>((state, span) => span == TimeSpan.FromDays(7))
                .When(s => s.Handle(new SubmitOrder {Total = 200}));
        }
        [Test]
        public void TestNullReferenceException()
        {
            Test.Initialize();
            var saga = new MySaga();
            Assert.DoesNotThrow(() => Test.Saga(saga));
        }
    }


    public class SagaThatDoesAReply : Saga.Saga<SagaThatDoesAReply.SagaThatDoesAReplyData>,
        IHandleMessages<MyRequest>
    {

        public class SagaThatDoesAReplyData : ContainSagaData
        {
             
        }

        public void Handle(MyRequest myRequest)
        {
            Bus.Reply(new MyReply());
        }
    }

    public class MyRequest
    {
    }


    public class MyReply
    {
    }

    public class MySaga : Saga.Saga<MySagaData>,
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

    public class StartsSaga : ICommand
    {
    }

    public class ResponseToOriginator : IMessage
    {
    }

    public interface Event : IEvent
    {
    }

    public class Command : ICommand
    {
    }

    public class DiscountPolicy : Saga.Saga<DiscountPolicyData>,
                                  IAmStartedByMessages<SubmitOrder>,
                                  IHandleTimeouts<SubmitOrder>
    {
        public void Handle(SubmitOrder message)
        {
            Data.CustomerId = message.CustomerId;
            Data.RunningTotal += message.Total;

            if (message.IsRemoteOrder)
                ProcessExternalOrder(message);
            else if (Data.RunningTotal >= 1000)
                ProcessOrderWithDiscount(message);
            else
                ProcessOrder(message);

            RequestUtcTimeout(TimeSpan.FromDays(7), message);
        }

        private void ProcessExternalOrder(SubmitOrder message)
        {
            Bus.Send<ProcessOrder>("remote.orderqueue", m =>
                                                            {
                                                                m.CustomerId = Data.CustomerId;
                                                                m.OrderId = message.OrderId;
                                                                m.Total = message.Total;
                                                            });
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
                                           m.Total = message.Total*(decimal) 0.9;
                                       });
        }
    }

    public class SubmitOrder : IMessage
    {
        public Guid OrderId { get; set; }
        public Guid CustomerId { get; set; }
        public decimal Total { get; set; }
        public bool IsRemoteOrder { get; set; }
    }

    public class ProcessOrder : IMessage
    {
        public Guid OrderId { get; set; }
        public Guid CustomerId { get; set; }
        public decimal Total { get; set; }
    }

    public class DiscountPolicyData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        public Guid CustomerId { get; set; }
        public decimal RunningTotal { get; set; }
    }
}
