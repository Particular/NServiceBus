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

    public class When_registering_additional_deserializers : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Two_endpoints_with_different_serializers_should_deserialize_the_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<CustomSerializationSender>(b => b.When(
                    (session, c) =>
                    {
                        var sendOptions = new SendOptions();
                        sendOptions.SetHeader("ContentType", "MyCustomSerializer");
                        return session.Send(new MyRequest());
                    }))
                .WithEndpoint<XmlCustomSerializationReceiver>()
                .Done(c => c.DeserializeCalled)
                .Run();

            Assert.True(context.DeserializeCalled);
        }

        public class Context : ScenarioContext
        {
            public bool HandlerGotTheRequest { get; set; }
            public bool SerializeCalled { get; set; }
            public bool DeserializeCalled { get; set; }
        }

        public class CustomSerializationSender : EndpointConfigurationBuilder
        {
            public CustomSerializationSender()
            {
                EndpointSetup<DefaultServer>(c => c.UseSerialization<MyCustomSerializer>())
                    .AddMapping<MyRequest>(typeof(XmlCustomSerializationReceiver));
            }
        }

        public class XmlCustomSerializationReceiver : EndpointConfigurationBuilder
        {
            public XmlCustomSerializationReceiver()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseSerialization<XmlSerializer>();
                    c.AddDeserializer<MyCustomSerializer>();
                });
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Context Context { get; set; }

                public Task Handle(MyRequest request, IMessageHandlerContext context)
                {
                    Context.HandlerGotTheRequest = true;
                    Context.DeserializeCalled = request.DeserializerCalled;
                    Context.SerializeCalled = request.SerializerCalled;
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class MyRequest : IMessage
        {
            public bool DeserializerCalled { get; set; }
            public bool SerializerCalled { get; set; }
        }

        class MyCustomSerializer : SerializationDefinition
        {
            protected override Func<IMessageMapper, IMessageSerializer> Configure(ReadOnlySettings settings)
            {
                return mapper => new MyCustomMessageSerializer();
            }
        }
        
        class MyCustomMessageSerializer : IMessageSerializer
        {
            public void Serialize(object message, Stream stream)
            {
                var serializer = new BinaryFormatter();

                ((MyRequest) message).SerializerCalled = true;

                serializer.Serialize(stream, message);
            }

            public object[] Deserialize(Stream stream, IList<Type> messageTypes = null)
            {
                var serializer = new BinaryFormatter();

                stream.Position = 0;
                var msg = serializer.Deserialize(stream);
                ((MyRequest) msg).DeserializerCalled = true;
                return new[]
                {
                    msg
                };
            }

            public string ContentType => "MyCustomSerializer";
        }
    }
}