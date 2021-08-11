namespace NServiceBus.AcceptanceTests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using EndpointTemplates;
    using MessageInterfaces;
    using NServiceBus.Serialization;
    using NUnit.Framework;
    using Settings;

    public class When_registering_custom_serializer : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_the_custom_serializer()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithCustomSerializer>(b => b.When(
                    (session, c) => session.SendLocal(new MyRequest())))
                .Done(c => c.HandlerGotTheRequest)
                .Run();

            Assert.IsTrue(context.SerializeCalled);
            Assert.IsTrue(context.DeserializeCalled);
        }

        public class Context : ScenarioContext
        {
            public bool HandlerGotTheRequest { get; set; }
            public bool SerializeCalled { get; set; }
            public bool DeserializeCalled { get; set; }
        }

        public class EndpointWithCustomSerializer : EndpointConfigurationBuilder
        {
            public EndpointWithCustomSerializer()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.UseSerialization<MySuperSerializer>();
                    c.GetSettings().Set((Context)r.ScenarioContext);
                });
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public MyRequestHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MyRequest request, IMessageHandlerContext context)
                {
                    testContext.HandlerGotTheRequest = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        [Serializable]
        public class MyRequest : IMessage
        {
        }

        class MySuperSerializer : SerializationDefinition
        {
            public override Func<IMessageMapper, IMessageSerializer> Configure(IReadOnlySettings settings)
            {
                return mapper => new MyCustomSerializer(settings.Get<Context>());
            }
        }

        class MyCustomSerializer : IMessageSerializer
        {
            public MyCustomSerializer(Context context)
            {
                this.context = context;
            }

            public void Serialize(object message, Stream stream)
            {
                context.SerializeCalled = true;

                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(MyRequest));

                serializer.Serialize(stream, message);
            }

            public object[] Deserialize(Stream stream, IList<Type> messageTypes = null)
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(MyRequest));

                stream.Position = 0;
                var msg = serializer.Deserialize(stream);

                context.DeserializeCalled = true;

                return new[]
                {
                    msg
                };
            }

            public string ContentType => "MyCustomSerializer";
            readonly Context context;
        }
    }
}