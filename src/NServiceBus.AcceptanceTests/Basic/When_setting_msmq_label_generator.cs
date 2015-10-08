namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_setting_msmq_label_generator : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_the_message_and_label()
        {
            DeleteAudit();
            try
            {
                await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                    .WithEndpoint<EndPoint>(b => b.When((bus, c) => bus.SendLocalAsync(new MyMessage
                    {
                        Id = c.Id
                    })))
                    .Done(c => c.WasCalled && ReadMessageLabel() == "MyLabel")
                    .Repeat(r => r.For<MsmqOnly>())
                    .Should(c => Assert.True(c.WasCalled, "The message handler should be called"))
                    .Run();
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

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public Guid Id { get; set; }

            public bool GeneratorWasCalled { get; set; }
        }


        public class EndPoint : EndpointConfigurationBuilder, IWantToRunBeforeConfigurationIsFinalized
        {
            public EndPoint()
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

            static Context Context { get; set; }

            public void Run(Configure config)
            {
                Context = config.Builder.Build<Context>();
            }


            string GetMessageLabel(IReadOnlyDictionary<string, string> headers)
            {
                Context.GeneratorWasCalled = true;
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

            public IBus Bus { get; set; }

            public Task Handle(MyMessage message)
            {
                if (Context.Id != message.Id)
                {
                    return Task.FromResult(0);
                }

                Context.WasCalled = true;

                return Task.FromResult(0);
            }
        }

        const string auditQueue = @".\private$\labelAuditQueue";
    }
}