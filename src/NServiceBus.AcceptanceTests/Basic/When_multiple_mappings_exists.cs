namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Threading;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_multiple_mappings_exists : NServiceBusAcceptanceTest
    {
        [Test]
        public void First_registration_should_be_the_final_destination()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Sender>(b => b.Given((bus, c) => bus.Send(new MyCommand1())))
                    .WithEndpoint<Receiver1>()
                    .WithEndpoint<Receiver2>()
                    .Done(c => c.WasCalled1 || c.WasCalled2)
                    .Run();

            Assert.IsTrue(context.WasCalled1);
            Assert.IsFalse(context.WasCalled2);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled1 { get; set; }
            public bool WasCalled2 { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>()
                    .AddMapping<MyCommand1>(typeof(Receiver1))
                    .AddMapping<MyCommand2>(typeof(Receiver2));
            }
        }

        public class Receiver1 : EndpointConfigurationBuilder
        {
            public Receiver1()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyMessageHandler : IHandleMessages<MyBaseCommand>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyBaseCommand message)
                {
                    Context.WasCalled1 = true;
                    Thread.Sleep(2000); // Just to be sure the other receiver is finished
                }
            }
        }

        public class Receiver2 : EndpointConfigurationBuilder
        {
            public Receiver2()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyMessageHandler : IHandleMessages<MyBaseCommand>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyBaseCommand message)
                {
                    Context.WasCalled2 = true;
                    Thread.Sleep(2000); // Just to be sure the other receiver is finished
                }
            }
        }

        [Serializable]
        public abstract class MyBaseCommand : ICommand
        {
        }

        [Serializable]
        public class MyCommand1 : MyBaseCommand
        {
        }

        [Serializable]
        public class MyCommand2 : MyBaseCommand
        {
        }
    }
}
