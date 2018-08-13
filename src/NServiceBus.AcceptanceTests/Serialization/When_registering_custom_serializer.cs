namespace NServiceBus.AcceptanceTests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
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
                var context = (Context) ScenarioContext;
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseSerialization<MySuperSerializer>();
                    c.GetSettings().Set(context);
                });
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Context Context { get; set; }

                public Task Handle(MyRequest request, IMessageHandlerContext context)
                {
                    Context.HandlerGotTheRequest = true;
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class MyRequest : IMessage
        {
        }

        class MySuperSerializer : SerializationDefinition
        {
            public override Func<IMessageMapper, IMessageSerializer> Configure(ReadOnlySettings settings)
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

                var serializer = new BinaryFormatter();
                serializer.Serialize(stream, message);
            }

            public object[] Deserialize(Stream stream, IList<Type> messageTypes = null)
            {
                var serializer = new BinaryFormatter();

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