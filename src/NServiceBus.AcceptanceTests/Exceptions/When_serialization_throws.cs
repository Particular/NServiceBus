namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Xml;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Faults;
    using NServiceBus.Features;
    using NServiceBus.MessageMutator;
    using NServiceBus.Unicast.Messages;
    using NUnit.Framework;

    public class When_serialization_throws : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_SerializationException()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new Message())))
                    .AllowExceptions()
                    .Done(c => c.ExceptionReceived)
                    .Run();
            Assert.AreEqual(typeof(SerializationException), context.ExceptionType);

            Assert.AreEqual(typeof(XmlException), context.InnerExceptionType);
#if (!DEBUG)
            StackTraceAssert.AreEqual(
@"at System.Xml.XmlTextReaderImpl.Throw(Exception e)
at System.Xml.XmlTextReaderImpl.ParseQName(Boolean isQName, Int32 startOffset, Int32& colonPos)
at System.Xml.XmlTextReaderImpl.ParseElement()
at System.Xml.XmlTextReaderImpl.ParseDocumentContent()
at System.Xml.XmlLoader.Load(XmlDocument doc, XmlReader reader, Boolean preserveWhitespace)
at System.Xml.XmlDocument.Load(XmlReader reader)
at NServiceBus.Serializers.XML.XmlMessageSerializer.Deserialize(Stream stream, IList`1 messageTypesToDeserialize)
at NServiceBus.DeserializeLogicalMessagesBehavior.Extract(TransportMessage physicalMessage)
at NServiceBus.DeserializeLogicalMessagesBehavior.Invoke(IncomingContext context, Action next)", context.InnerExceptionStackTrace);
#endif
        }

        public class Context : ScenarioContext
        {
            public bool ExceptionReceived { get; set; }
            public string StackTrace { get; set; }
            public Type ExceptionType { get; set; }
            public string InnerExceptionStackTrace { get; set; }
            public Type InnerExceptionType { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.RegisterComponents(c =>
                    {
                        c.ConfigureComponent<CustomFaultManager>(DependencyLifecycle.SingleInstance);
                        c.ConfigureComponent<CorruptionMutator>(DependencyLifecycle.InstancePerCall);
                    });
                    b.DisableFeature<TimeoutManager>();
                })
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0;
                    });
            }

            class CustomFaultManager : IManageMessageFailures
            {
                public Context Context { get; set; }

                public void SerializationFailedForMessage(TransportMessage message, Exception e)
                {
                    Context.ExceptionType = e.GetType();
                    Context.StackTrace = e.StackTrace;
                    if (e.InnerException != null)
                    {
                        Context.InnerExceptionType = e.InnerException.GetType();
                        Context.InnerExceptionStackTrace = e.InnerException.StackTrace;
                    }
                    Context.ExceptionReceived = true;
                }

                public void ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
                {
                }

                public void Init(Address address)
                {
                }
            }

            class CorruptionMutator : IMutateTransportMessages
            {
                [MethodImpl(MethodImplOptions.NoInlining)]
                public void MutateIncoming(TransportMessage transportMessage)
                {
                    transportMessage.Body[1]++;
                }
                
                [MethodImpl(MethodImplOptions.NoInlining)]
                public void MutateOutgoing(LogicalMessage logicalMessage, TransportMessage transportMessage)
                {
                }
            }

            class Handler : IHandleMessages<Message>
            {
                [MethodImpl(MethodImplOptions.NoInlining)]
                public void Handle(Message message)
                {
                }
            }
        }

        [Serializable]
        public class Message : IMessage
        {
        }
    }
    
}