namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Features;
    using NServiceBus.UnitOfWork;
    using NUnit.Framework;

    public class When_handling_current_message_later : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_commit_unit_of_work_and_execute_subsequent_handlers()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<MyEndpoint>(b => b.Given((bus, c) => bus.SendLocalAsync(new SomeMessage { Id = c.Id })))
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
                    b.DisableFeature<TimeoutManager>();
                    b.DisableFeature<SecondLevelRetries>();
                    b.ExecuteTheseHandlersFirst(typeof(FirstHandler), typeof(SecondHandler));
                })
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0;
                    });
            }

            class CheckUnitOfWorkOutcome : IManageUnitsOfWork
            {
                public Context Context { get; set; }

                public void Begin()
                {
                }

                public void End(Exception ex = null)
                {
                    Context.UoWCommited = (ex == null);
                }
            }

            class FirstHandler : IHandleMessages<SomeMessage>
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public Task Handle(SomeMessage message)
                {
                    if (message.Id != Context.Id)
                    {
                        return Task.FromResult(0);
                    }
                    Context.FirstHandlerInvocationCount++;

                    if (Context.FirstHandlerInvocationCount == 1)
                    {
                        return Bus.HandleCurrentMessageLaterAsync();
                    }

                    return Task.FromResult(0);
                }
            }

            class SecondHandler : IHandleMessages<SomeMessage>
            {
                public Context Context { get; set; }
                public Task Handle(SomeMessage message)
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