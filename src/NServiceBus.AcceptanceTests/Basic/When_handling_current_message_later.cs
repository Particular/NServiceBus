namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using UnitOfWork;

    public class When_handling_current_message_later : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_commit_unit_of_work_and_execute_subsequent_handlers()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<MyEndpoint>(b => b.When((session, c) => session.SendLocal(new SomeMessage
                {
                    Id = c.Id
                })))
                .Done(c => c.Done)
                .Run();

            Assert.True(context.UoWCommited);
            Assert.That(context.FirstHandlerInvocationCount, Is.EqualTo(2));
            Assert.That(context.SecondHandlerInvocationCount, Is.EqualTo(1));
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public bool Done { get; set; }
            public int FirstHandlerInvocationCount { get; set; }
            public int SecondHandlerInvocationCount { get; set; }
            public bool UoWCommited { get; set; }
        }

        public class MyEndpoint : EndpointConfigurationBuilder
        {
            public MyEndpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.RegisterComponents(r => r.ConfigureComponent<CheckUnitOfWorkOutcome>(DependencyLifecycle.InstancePerCall));
                    b.ExecuteTheseHandlersFirst(typeof(FirstHandler), typeof(SecondHandler));
                });
            }

            class CheckUnitOfWorkOutcome : IManageUnitsOfWork
            {
                public Context Context { get; set; }

                public Task Begin()
                {
                    return Task.FromResult(0);
                }

                public Task End(Exception ex = null)
                {
                    Context.UoWCommited = ex == null;
                    return Task.FromResult(0);
                }
            }

            class FirstHandler : IHandleMessages<SomeMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    if (message.Id != TestContext.Id)
                    {
                        return Task.FromResult(0);
                    }
                    TestContext.FirstHandlerInvocationCount++;

                    if (TestContext.FirstHandlerInvocationCount == 1)
                    {
                        return context.HandleCurrentMessageLater();
                    }

                    return Task.FromResult(0);
                }
            }

            class SecondHandler : IHandleMessages<SomeMessage>
            {
                public Context Context { get; set; }

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    if (message.Id != Context.Id)
                    {
                        return Task.FromResult(0);
                    }

                    Context.SecondHandlerInvocationCount++;
                    Context.Done = true;

                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class SomeMessage : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}