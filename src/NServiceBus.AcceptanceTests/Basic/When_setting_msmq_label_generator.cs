namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_setting_msmq_label_generator : NServiceBusAcceptanceTest
    {
        const string auditQueue = @".\private$\labelAuditQueue";

        [Test]
        public void Should_receive_the_message_and_label()
        {
            DeleteAudit();
            try
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
                    .Done(c => c.WasCalled && ReadMessageLabel() == "MyLabel")
                    .Repeat(r => r.For<MsmqOnly>())
                    .Run();

                Assert.True(context.WasCalled, "The message handler should be called");
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
                if (message != null)
                {
                    return message.Label;
                }
            }
            return null;
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public Guid Id { get; set; }

            public bool GeneratorWasCalled { get; set; }
        }


        public class EndPoint : EndpointConfigurationBuilder,IWantToRunBeforeConfigurationIsFinalized
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
                    c.AuditProcessedMessagesTo("labelAuditQueue");
                    c.UseTransport<MsmqTransport>().ApplyLabelToMessages(GetMessageLabel);
                });
            }

            static Context Context { get; set; }


            string GetMessageLabel(IReadOnlyDictionary<string, string> headers)
            {
                Context.GeneratorWasCalled = true;
                return "MyLabel";
            }

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
