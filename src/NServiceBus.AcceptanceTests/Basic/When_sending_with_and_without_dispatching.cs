namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    // TODO: Probably belongs in different namespace
    public class When_sending_with_and_without_dispatching : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_the_correct_number_of_message()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<Sender>(b => b.When(async (factory, c) =>
                {
                    var sendOptions = new SendOptions();

                    // Nothing should be dispatched
                    using (var bus = factory.CreateBusSession(autoDispatch: false))
                    {
                        await bus.Send(new MyMessage { Id = c.Id }, sendOptions);
                        await bus.Send(new MyMessage { Id = c.Id }, sendOptions);
                    }

                    // Everything should be dispatched.
                    using (var bus = factory.CreateBusSession(autoDispatch: false))
                    {
                        await bus.Send(new MyMessage { Id = c.Id }, sendOptions);
                        await bus.Send(new MyMessage { Id = c.Id }, sendOptions);
                        await bus.Dispatch();
                    }

                    // Only immediate dispatch should be dispatched.
                    using (var bus = factory.CreateBusSession(autoDispatch: false))
                    {
                        var immediateDispatchOptions = new SendOptions();
                        immediateDispatchOptions.RequireImmediateDispatch();

                        await bus.Send(new MyMessage { Id = c.Id }, immediateDispatchOptions);
                        await bus.Send(new MyMessage { Id = c.Id }, sendOptions);
                    }

                    // Everything should be dispatched.
                    using (var bus = factory.CreateBusSession())
                    {
                        await bus.Send(new MyMessage { Id = c.Id }, sendOptions);
                        await bus.Send(new MyMessage { Id = c.Id }, sendOptions);
                    }
                }))
                .WithEndpoint<Receiver>()
                .Done(c => c.TimesCalled > 4)
                .Run();

            Assert.AreEqual(5, context.TimesCalled, "The message handler should only be invoked five times");
        }

        public class Context : ScenarioContext
        {
            long timesCalled;

            public long TimesCalled => Interlocked.Read(ref timesCalled);

            public Guid Id { get; set; }

            public void Called()
            {
                Interlocked.Increment(ref timesCalled);
            }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.AddHeaderToAllOutgoingMessages("MyStaticHeader", "StaticHeaderValue");
                }).AddMapping<MyMessage>(typeof(Receiver));
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    if (Context.Id != message.Id)
                        return Task.FromResult(0);

                    Context.Called();

                    return Task.FromResult(0);
                }
            }
        }

        public class MyMessage : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}