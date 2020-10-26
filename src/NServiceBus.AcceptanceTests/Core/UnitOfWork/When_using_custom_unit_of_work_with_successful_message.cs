namespace NServiceBus.AcceptanceTests.UnitOfWork
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.UnitOfWork;
    using NUnit.Framework;

    public class When_using_custom_unit_of_work_with_successful_message : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_execute_uow()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithCustomUnitOfWork>(g => g.When(b => b.SendLocal(new MyMessage())))
                .Done(c => c.BeginCalled && c.EndCalled)
                .Run();

            Assert.True(context.BeginCalled, "Unit of work should have been executed");
            Assert.True(context.EndCalled, "Unit of work should have been executed");
            Assert.IsNull(context.EndException, "Exception was provided to unit of work but should not have been");
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public bool BeginCalled { get; set; }
            public bool EndCalled { get; set; }
            public Exception EndException { get; set; }
        }

        public class EndpointWithCustomUnitOfWork : EndpointConfigurationBuilder
        {
            public EndpointWithCustomUnitOfWork()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.RegisterComponents(container => container.ConfigureComponent<CustomUnitOfWork>(DependencyLifecycle.InstancePerCall));
                });
            }

            class CustomUnitOfWork : IManageUnitsOfWork
            {
                public CustomUnitOfWork(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Begin()
                {
                    testContext.BeginCalled = true;
                    return Task.FromResult(0);
                }

                public Task End(Exception ex = null)
                {
                    testContext.EndCalled = true;
                    testContext.EndException = ex;
                    return Task.FromResult(0);
                }

                Context testContext;
            }

            class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public MyMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    testContext.Done = true;

                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}