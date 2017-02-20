namespace NServiceBus.AcceptanceTests.Core.UnitOfWork.TransactionScope
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_using_custom_unit_of_work : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_allow_unit_of_work_to_be_shared_between_handlers()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(g => g.When(b => b.SendLocal(new MyMessage())))
                .Done(c => c.Done)
                .Run();

            Assert.AreEqual(2, context.Counter, "Both handlers should increment value on same instance");
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public int Counter { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.UseTransport(r.GetTransportType())
                        .Transactions(TransportTransactionMode.ReceiveOnly);
                    c.Pipeline.Register(b => new CustomUnitOfWorkBehavior(b.Build<Context>()), "Manages custom unit of work.");
                });
            }

            class CustomUnitOfWorkBehavior : Behavior<IUnitOfWorkContext>
            {
                Context testContext;

                public CustomUnitOfWorkBehavior(Context testContext)
                {
                    this.testContext = testContext;
                }

                public override async Task Invoke(IUnitOfWorkContext context, Func<Task> next)
                {
                    var uow = new UnitOfWork();
                    context.Extensions.Set(uow);

                    await next().ConfigureAwait(false);

                    testContext.Counter = uow.Counter;
                    testContext.Done = true;
                }
            }

            class UnitOfWork
            {
                public int Counter { get; set; }
            }

            class FirstMessageHandler : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    var uow = context.Extensions.Get<UnitOfWork>();
                    uow.Counter++;
                    return Task.FromResult(0);
                }
            }

            class SecondMessageHandler : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    var uow = context.Extensions.Get<UnitOfWork>();
                    uow.Counter++;
                    return Task.FromResult(0);
                }
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}