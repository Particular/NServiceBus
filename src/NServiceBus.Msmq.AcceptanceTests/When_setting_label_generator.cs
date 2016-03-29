namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Settings;

    public class When_setting_label_generator : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_the_message_and_label()
        {
            DeleteAudit();
            try
            {
                await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                    .WithEndpoint<Endpoint>(b => b.When((session, c) => session.SendLocal(new MyMessage
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
            {
                using (var message = queue.Receive(TimeSpan.FromSeconds(5)))
                {
                    return message?.Label;
                }
            }
        }

        const string auditQueue = @".\private$\labelAuditQueue";

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public Guid Id { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder, IWantToRunBeforeConfigurationIsFinalized
        {
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

            public void Run(SettingsHolder config)
            {
            }

            string GetMessageLabel(IReadOnlyDictionary<string, string> headers)
            {
                return "MyLabel";
            }

            static bool initialized;
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
                {
                    return Task.FromResult(0);
                }

                Context.WasCalled = true;

                return Task.FromResult(0);
            }
        }
    }
}