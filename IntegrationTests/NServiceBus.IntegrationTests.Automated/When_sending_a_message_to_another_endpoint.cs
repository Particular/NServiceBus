namespace NServiceBus.IntegrationTests.Automated
{
    using System;
    using System.Collections.Generic;
    using EndpointTemplates;
    using NUnit.Framework;
    using Support;

    [TestFixture]
    [ForAllTransports]
    public class When_sending_a_message_to_another_endpoint
    {
        [Test]
        public void Should_receive_the_message()
        {
            var context = new ReceiveContext();

            Scenario.Define()
                .WithEndpointBehaviour<SendBehavior>()
                .WithEndpointBehaviour<ReceiveBehavior>(context)
              .Run();

            Assert.True(context.WasCalled);
        }

        public class ReceiveContext : BehaviorContext
        {
            public bool WasCalled { get; set; }
        }

        public class SendBehavior : BehaviorFactory
        {
            public EndpointBehavior Get()
            {
                return new ScenarioBuilder("Sender")
                    .EndpointSetup<DefaultServer>()
                    .AddMapping<MyMessage>("Receiver")
                    .When(bus => bus.Send(new MyMessage()))
                    .CreateScenario();
            }
        }

        public class ReceiveBehavior : BehaviorFactory
        {
            public EndpointBehavior Get()
            {
                return new ScenarioBuilder("Receiver")
                    .EndpointSetup<DefaultServer>()
                    .Done((ReceiveContext context) => context.WasCalled)
                    .CreateScenario();
            }
        }

        public class MyMessage : IMessage
        {
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            private readonly ReceiveContext context;

            public MyMessageHandler(ReceiveContext context)
            {
                this.context = context;
            }

            public void Handle(MyMessage message)
            {
                this.context.WasCalled = true;
            }
        }
    }

    public class Scenario
    {
        readonly IList<BehaviorDescriptor> behaviours = new List<BehaviorDescriptor>();

        private Scenario()
        {
        }

        public static Scenario Define()
        {
            return new Scenario();
        }

        public  Scenario WithEndpointBehaviour<T>() where T:BehaviorFactory
        {
            behaviours.Add(new BehaviorDescriptor(new BehaviorContext(), Activator.CreateInstance<T>()));
            return this;
        }

        public Scenario WithEndpointBehaviour<T>(BehaviorContext context) where T : BehaviorFactory
        {
            behaviours.Add(new BehaviorDescriptor(context, Activator.CreateInstance<T>()));
            return this;
        }

        public void Run()
        {
            ScenarioRunner.Run(behaviours);
        }
    }

    public class BehaviorDescriptor
    {
        public BehaviorDescriptor(BehaviorContext context, BehaviorFactory factory)
        {
            this.Context = context;
            this.Factory = factory;
        }

        public BehaviorContext Context { get; private set; }

        public BehaviorFactory Factory { get; private set; }
    }
}
