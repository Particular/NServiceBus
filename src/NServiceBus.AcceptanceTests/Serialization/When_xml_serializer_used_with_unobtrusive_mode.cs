namespace NServiceBus.AcceptanceTests.Serialization
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_xml_serializer_used_with_unobtrusive_mode : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Message_should_be_received_with_deserialized_payload()
        {
            var expectedData = 1;

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(c => c.When(s => s.Send(new MyCommand { Data = expectedData })))
                .WithEndpoint<Receiver>()
                .Done(c => c.WasCalled)
                .Run(TimeSpan.FromSeconds(10));

            Assert.AreEqual(expectedData, context.Data);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public int Data { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                    {
                        c.Conventions().DefiningCommandsAs(t => t.Namespace != null && t.FullName.EndsWith("Command"));
                        c.UseSerialization<XmlSerializer>();
                    }).AddMapping<MyCommand>(typeof(Receiver))
                    .ExcludeType<MyCommand>(); // remove that type from assembly scanning to simulate what would happen with true unobtrusive mode
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Conventions().DefiningCommandsAs(t => t.Namespace != null && t.FullName.EndsWith("Command"));
                    c.UseSerialization<XmlSerializer>();
                })
                .ExcludeType<MyCommand>(); // remove that type from assembly scanning to simulate what would happen with true unobtrusive mode
            }

            public class MyMessageHandler : IHandleMessages<ICommand>
            {
                public Context Context { get; set; }

                public Task Handle(ICommand message, IMessageHandlerContext context)
                {
                    Context.Data = ((MyCommand)message).Data;
                    Context.WasCalled = true;
                    return Task.FromResult(0);
                }
            }
        }

        public interface ICommand { }

        public class MyCommand : ICommand
        {
            public int Data { get; set; }
        }
    }
}