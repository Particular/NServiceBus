namespace NServiceBus.AcceptanceTests.BasicMessaging
{
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using UnitOfWork;

    public class When_handling_current_message_later : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_commit_unit_of_work()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<MyEndpoint>(b => b.Given(bus => bus.SendLocal(new SomeMessage())))
                .Done(c => c.Done)
                .Run(TimeSpan.FromSeconds(10));

            Assert.True(context.UoWCommited);
        }

        [Test]
        public void Should_not_execute_subsequent_handlers()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<MyEndpoint>(b => b.Given(bus => bus.SendLocal(new SomeMessage())))
                .Done(c => c.Done)
                .Run();

            Assert.That(context.FirstHandlerInvocationCount, Is.EqualTo(2));
            Assert.That(context.SecondHandlerInvocationCount, Is.EqualTo(1));
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }

            public int FirstHandlerInvocationCount { get; set; }

            public int SecondHandlerInvocationCount { get; set; }
            public bool UoWCommited { get; set; }
        }

        public class MyEndpoint : EndpointConfigurationBuilder
        {
            public MyEndpoint()
            {
                EndpointSetup<DefaultServer>(b => b.RegisterComponents(r => r.ConfigureComponent<CheckUnitOfWorkOutcome>(DependencyLifecycle.InstancePerCall)));
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

            class FirstHandler : IHandleMessages<SomeMessage>
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public void Handle(SomeMessage message)
                {
                    Context.FirstHandlerInvocationCount++;

                    if (Context.FirstHandlerInvocationCount == 1)
                    {
                        Bus.HandleCurrentMessageLater();
                    }
                }
            }

            class SecondHandler : IHandleMessages<SomeMessage>
            {
                public Context Context { get; set; }
                public void Handle(SomeMessage message)
                {
                    Context.SecondHandlerInvocationCount++;
                    Context.Done = true;
                }
            }
        }
        [Serializable]
        public class SomeMessage : IMessage { }


    }

}