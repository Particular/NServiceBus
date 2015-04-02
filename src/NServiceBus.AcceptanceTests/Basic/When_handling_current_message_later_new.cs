namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Features;
    using NServiceBus.UnitOfWork;
    using NUnit.Framework;

    public class When_handling_current_message_later_new : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_commit_unit_of_work_and_execute_subsequent_handlers()
        {
            var context = new Context
            {
                Id = Guid.NewGuid()
            };

            Scenario.Define(context)
                .WithEndpoint<MyEndpoint>(b => b.Given((bus, c) => bus.SendLocal(new SomeMessage{Id = c.Id})))
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
                })
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0;
                    });
            }

            class EnsureOrdering : ISpecifyMessageHandlerOrdering
            {
                public void SpecifyOrder(Order order)
                {
                    order.Specify(First<FirstHandler>.Then<SecondHandler>());
                }
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

            class FirstHandler : IProcessCommands<SomeMessage>
            {
                public Context Context { get; set; }

                public void Handle(SomeMessage message, ICommandContext context)
                {
                    if (message.Id != Context.Id)
                    {
                        return;
                    }
                    Context.FirstHandlerInvocationCount++;

                    if (Context.FirstHandlerInvocationCount == 1)
                    {
                        context.HandleCurrentMessageLater();
                    }
                }
            }

            class SecondHandler : IProcessCommands<SomeMessage>
            {
                public Context Context { get; set; }
                public void Handle(SomeMessage message, ICommandContext context)
                {
                    if (message.Id != Context.Id)
                    {
                        return;
                    }
                    Context.SecondHandlerInvocationCount++;
                    Context.Done = true;
                }
            }
        }
        [Serializable]
        public class SomeMessage : ICommand 
        {
            public Guid Id { get; set; }
        }

    }

}