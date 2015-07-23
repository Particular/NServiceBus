namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Linq;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_starting_up_the_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_log_warning_if_queue_is_configured_with_anon_and_everyone_permissions()
        {
            var context = new Context
            {
                Id = Guid.NewGuid()
            };
            Scenario.Define(context)
                .WithEndpoint<EndPoint>(b => b.Given((bus, c) => bus.SendLocal(new MyMessage
                {
                    Id = c.Id
                })))
                .Done(c => c.WasCalled)
                .Run();

            var logItem = context.GetAllLogs().FirstOrDefault(item => item.Message.Contains("permissions"));
            Assert.IsNotNull(logItem);
            StringAssert.Contains(@"is running with [Everyone] and [NT AUTHORITY\ANONYMOUS LOGON] permissions. Consider changing those, as required", logItem.Message);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public Guid Id { get; set; }

            public bool GeneratorWasCalled { get; set; }
        }

        public class EndPoint : EndpointConfigurationBuilder, IWantToRunBeforeConfigurationIsFinalized
        {
            static bool initialized;
            public EndPoint()
            {
                if (initialized)
                {
                    return;
                }
                initialized = true;
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseTransport<MsmqTransport>();
                });
            }

            static Context Context { get; set; }

            public void Run(Configure config)
            {
                Context = config.Builder.Build<Context>();
            }
        }

        [Serializable]
        public class MyMessage : ICommand
        {
            public Guid Id { get; set; }
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }

            public IBus Bus { get; set; }

            public void Handle(MyMessage message)
            {
                if (Context.Id != message.Id)
                    return;

                Context.WasCalled = true;
            }
        }
    }
}