namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
    using NServiceBus.Serialization;
    using NUnit.Framework;

    public class When_registering_custom_serializer : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_register_via_type()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<EndpointViaType>(b => b.Given(
                    (bus, c) => bus.SendLocal(new MyRequest())))
                .Done(c => c.HandlerGotTheRequest)
                .Run();

            Assert.IsTrue(context.SerializeCalled);
            Assert.IsTrue(context.DeserializeCalled);
        }

        [Test]
        public void Should_register_via_definition()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<EndpointViaDefinition>(b => b.Given(
                    (bus, c) => bus.SendLocal(new MyRequest())))
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

        public class EndpointViaType : EndpointConfigurationBuilder
        {
            public EndpointViaType()
            {
                EndpointSetup<DefaultServer>(c => c.UseSerialization(typeof(MyCustomSerializer)));
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Context Context { get; set; }

                public void Handle(MyRequest request)
                {
                    Context.HandlerGotTheRequest = true;
                }
            }
        }

        public class EndpointViaDefinition : EndpointConfigurationBuilder
        {
            public EndpointViaDefinition()
            {
                EndpointSetup<DefaultServer>(c => c.UseSerialization(typeof(MySuperSerializer)));
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Context Context { get; set; }

                public void Handle(MyRequest request)
                {
                    Context.HandlerGotTheRequest = true;
                }
            }
        }

        [Serializable]
        public class MyRequest : IMessage
        {
        }

        class MySuperSerializer : SerializationDefinition
        {
            protected override Type ProvidedByFeature()
            {
                return typeof(MySuperSerializerFeature);
            }
        }

        class MySuperSerializerFeature : Feature
        {
            public MySuperSerializerFeature()
            {
                EnableByDefault();
            }

            protected override void Setup(FeatureConfigurationContext context)
            {
                context.Container.ConfigureComponent<MessageMapper>(DependencyLifecycle.SingleInstance);
                context.Container.ConfigureComponent<MyCustomSerializer>(DependencyLifecycle.SingleInstance);
            }
        }

        class MyCustomSerializer : IMessageSerializer
        {
            object lastMessage;

            public Context Context { get; set; }

            public void Serialize(object message, Stream stream)
            {
                Context.SerializeCalled = true;
                stream.WriteByte(1); //Need this because we internally we check to see if body is 0
                lastMessage = message;
            }

            public object[] Deserialize(Stream stream, IList<Type> messageTypes = null)
            {
                Context.DeserializeCalled = true;

                return new[]
                {
                    lastMessage
                };
            }

            public string ContentType
            {
                get { return "MyCustomSerializer"; }
            }
        }
    }
}
