#pragma warning disable CS0618
namespace NServiceBus.AcceptanceTests.UnitOfWork
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.UnitOfWork;
    using NUnit.Framework;

    public class When_using_custom_unit_of_work_with_failed_message : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_execute_uow_and_provide_exception_details()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithCustomUnitOfWork>(g =>
                {
                    g.DoNotFailOnErrorMessages();
                    g.When(b => b.SendLocal(new MyMessage()));
                })
                .Done(c => c.BeginCalled && c.EndCalled)
                .Run();

            Assert.True(context.BeginCalled, "Unit of work should have been executed");
            Assert.True(context.EndCalled, "Unit of work should have been executed");
            Assert.That(context.EndException, Is.InstanceOf<SimulatedException>().And.Message.Contain("Something went wrong"), "Exception was not provided but should have been");
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
                    c.RegisterComponents(services => services.AddSingleton<IManageUnitsOfWork, CustomUnitOfWork>());
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
                    return Task.CompletedTask;
                }

                public Task End(Exception ex = null)
                {
                    testContext.EndCalled = true;
                    testContext.EndException = ex;
                    return Task.CompletedTask;
                }

                Context testContext;
            }

            class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    throw new SimulatedException("Something went wrong");
                }
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}