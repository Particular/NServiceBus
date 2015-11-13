namespace NServiceBus.AcceptanceTests.Msmq
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Settings;
    using NUnit.Framework;

    public class When_setting_msmq_label_generator : NServiceBusAcceptanceTest
    {
        const string auditQueue = @".\private$\labelAuditQueue";

        [Test]
        public async Task Should_receive_the_message_and_label()
        {
            DeleteAudit();
            try
            {
                await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                    .WithEndpoint<Endpoint>(b => b.When((bus, c) => bus.SendLocal(new MyMessage
                    {
                        Id = c.Id
                    })))
                    .Done(c => c.WasCalled)
                    .Run();
                Assert.AreEqual("MyLabel", ReadMessageLabel());
            }
            finally
            {
                DeleteAudit();
            }
        }

        static void DeleteAudit()
        {
            if (MessageQueue.Exists(auditQueue))
            {
                MessageQueue.Delete(auditQueue);
            }
        }

        static string ReadMessageLabel()
        {
            if (!MessageQueue.Exists(auditQueue))
            {
                return null;
            }
            using (var queue = new MessageQueue(auditQueue))
            using (var message = queue.Receive(TimeSpan.FromSeconds(5)))
            {
                return message?.Label;
            }
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public Guid Id { get; set; }

            public bool GeneratorWasCalled { get; set; }
        }


        public class Endpoint : EndpointConfigurationBuilder, IFinalizeConfiguration
        {
            static bool initialized;
            bool generatorWasCalled;

            public Endpoint()
            {
                if (initialized)
                {
                    return;
                }
                initialized = true;
                EndpointSetup<DefaultServer>(c =>
                {
                    c.AuditProcessedMessagesTo("labelAuditQueue");
                    c.UseTransport<MsmqTransport>().ApplyLabelToMessages(GetMessageLabel);
                });
            }

            public class StartHandler : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }
                public ReadOnlySettings Settings { get; set; }

                public Task StartAsync(IBusContext context)
                {
                    Context.GeneratorWasCalled = Settings.Get<bool>("GeneratorWasCalled");
                    return Task.FromResult(0);
                }

                public Task StopAsync(IBusContext context)
                {
                    return Task.FromResult(0);
                }
            }

            static Context Context { get; set; }

            string GetMessageLabel(IReadOnlyDictionary<string, string> headers)
            {
                generatorWasCalled = true;
                return "MyLabel";
            }

            public void Run(SettingsHolder config)
            {
                config.Set("GeneratorWasCalled", generatorWasCalled);
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

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                if (Context.Id != message.Id)
                    return Task.FromResult(0);

                Context.WasCalled = true;

                return Task.FromResult(0);
            }
        }
    }


}