#pragma warning disable CS0618
namespace NServiceBus.AcceptanceTests.UnitOfWork
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.UnitOfWork;
    using NUnit.Framework;

    public class When_using_custom_unit_of_work_with_wrap_handlers_in_scope : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_fail()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithCustomUnitOfWork>(g =>
                {
                    g.DoNotFailOnErrorMessages();
                    g.When(b => b.SendLocal(new MyMessage()));
                })
                .Done(c => c.FailedMessages.Any())
                .Run();

            Assert.False(context.ShouldNeverBeCalled, "Unit of work should have been executed");
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public bool ShouldNeverBeCalled { get; set; }
        }

        public class EndpointWithCustomUnitOfWork : EndpointConfigurationBuilder
        {
            public EndpointWithCustomUnitOfWork()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.UnitOfWork().WrapHandlersInATransactionScope();

                    c.RegisterComponents(services => services.AddSingleton<IManageUnitsOfWork, CustomUnitOfWork>());
                });
            }

            class CustomUnitOfWork : IManageUnitsOfWork
            {
                TransactionScope transactionScope;
                public Task Begin()
                {
                    // this only works because we are not using the async state machine
                    transactionScope = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled);
                    return Task.CompletedTask;
                }

                public Task End(Exception ex = null)
                {
                    transactionScope.Complete();
                    return Task.CompletedTask;
                }
            }

            class MyMessageHandler : IHandleMessages<MyMessage>
            {
                Context testContext;

                public MyMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    testContext.ShouldNeverBeCalled = true;
                    return Task.CompletedTask;
                }
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}