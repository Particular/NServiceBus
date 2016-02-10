namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.Serialization;
    using NServiceBus.Settings;
    using NUnit.Framework;

    public class When_registering_custom_serializer : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_the_custom_serializer()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithCustomSerializer>(b => b.When(
                    (session, c) => session.SendLocal(new MyRequest
                    {
                        Serialized = false,
                        Deserialized = false
                    })))
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
                EndpointSetup<DefaultServer>(c => c.UseSerialization<MySuperSerializer>());
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Context Context { get; set; }

                public Task Handle(MyRequest request, IMessageHandlerContext context)
                {
                    Context.HandlerGotTheRequest = true;
                    Context.DeserializeCalled = request.Deserialized;
                    Context.SerializeCalled = request.Serialized;
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class MyRequest : IMessage
        {
            public bool Serialized { get; set; }
            public bool Deserialized { get; set; }
        }

        class MySuperSerializer : SerializationDefinition
        {
            public override Func<IMessageMapper, IMessageSerializer> Configure(ReadOnlySettings settings)
            {
                return mapper => new MyCustomSerializer();
            }
        }
        
        class MyCustomSerializer : IMessageSerializer
        {
            public void Serialize(object message, Stream stream)
            {
                ((MyRequest) message).Serialized = true;

                var serializer = new BinaryFormatter();
                serializer.Serialize(stream, message);
            }

            public object[] Deserialize(Stream stream, IList<Type> messageTypes = null)
            {
                var serializer = new BinaryFormatter();

                stream.Position = 0;
                var msg = serializer.Deserialize(stream);

                ((MyRequest) msg).Deserialized = true;

                return new[]
                {
                    msg
                };
            }

            public string ContentType => "MyCustomSerializer";
        }
    }
}
