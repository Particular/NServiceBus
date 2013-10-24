namespace NServiceBus.AcceptanceTests.BasicMessaging
{
    using System.Linq;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NHibernate.Cfg;
    using NUnit.Framework;
    using Environment = System.Environment;

    public class When_handling_current_message_later : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_commit_unit_of_work()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<MyEndpoint>(b => b.Given(bus => bus.Send(Address.Local, new MyEndpoint.SomeMessage())))
                .Done(c => c.Done)
                .Run();

            Assert.That(context.AnotherMessageReceivedCount, Is.EqualTo(2), 
                "First handler sends a message to self, which should result in AnotherMessage being dispatched twice");
        }

        [Test, Description("NOTE the double negation - we should probably modify this behavior somehow")]
        public void Should_not_not_execute_subsequent_handlers()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<MyEndpoint>(b => b.Given(bus => bus.Send(Address.Local, new MyEndpoint.SomeMessage())))
                .Done(c => c.Done)
                .Run();

            Assert.That(context.ThirdHandlerInvokedCount, Is.EqualTo(2), 
                "Since calling HandleCurrentMessageLater does not discontinue message dispatch, the third handler should be called twice as well");
        }

        [Test]
        public void Handlers_are_executed_in_the_right_order()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<MyEndpoint>(b => b.Given(bus => bus.Send(Address.Local, new MyEndpoint.SomeMessage())))
                .Done(c => c.Done)
                .Run();

            var events = context.Events;
            CollectionAssert
                .AreEquivalent(new[]
                               {
                                   "FirstHandler:Executed",
                                   "SecondHandler:Sending message to the back of the queue",
                                   "ThirdHandler:Executed",
                                   "FirstHandler:Executed",
                                   "SecondHandler:Handling the message this time",
                                   "ThirdHandler:Executed",
                               },
                    events);
        }

        public class Context : ScenarioContext
        {
            public string[] Events { get; set; }
            
            public bool MessageHasBeenRequeued { get; set; }

            public bool Done
            {
                get { return ThirdHandlerInvokedCount >= 1 && AnotherMessageReceivedCount == 2; }
            }

            public int AnotherMessageReceivedCount { get; set; }
            
            public int ThirdHandlerInvokedCount { get; set; }
        }

        public class MyEndpoint : EndpointConfigurationBuilder
        {
            public MyEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class SomeMessage : IMessage { }

            public class AnotherMessage : IMessage { }

            class EnsureOrdering : ISpecifyMessageHandlerOrdering
            {
                public void SpecifyOrder(Order order)
                {
                    order.Specify(First<FirstHandler>.Then<SecondHandler>().AndThen<ThirdHandler>());
                }
            }

            class FirstHandler : IHandleMessages<SomeMessage>, IHandleMessages<AnotherMessage>
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public void Handle(SomeMessage message)
                {
                    Context.RegisterEvent("FirstHandler:Executed");
                    Bus.SendLocal(new AnotherMessage());
                }

                public void Handle(AnotherMessage message)
                {
                    Context.AnotherMessageReceivedCount++;
                }
            }

            class SecondHandler : IHandleMessages<SomeMessage>
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }
                public void Handle(SomeMessage message)
                {
                    if (!Context.MessageHasBeenRequeued)
                    {
                        Context.RegisterEvent("SecondHandler:Sending message to the back of the queue");
                        Bus.HandleCurrentMessageLater();
                        Context.MessageHasBeenRequeued = true;
                    }
                    else
                    {
                        Context.RegisterEvent("SecondHandler:Handling the message this time");
                    }
                }
            }

            class ThirdHandler : IHandleMessages<SomeMessage>
            {
                public Context Context { get; set; }
                public void Handle(SomeMessage message)
                {
                    Context.RegisterEvent("ThirdHandler:Executed");
                    Context.ThirdHandlerInvokedCount++;
                }
            }
        }
    }

    static class ContextEx
    {
        public static void RegisterEvent(this When_handling_current_message_later.Context context, string description)
        {
            context.Events = context.Events
                .Concat(new[] {description})
                .ToArray();
        }
    }
}