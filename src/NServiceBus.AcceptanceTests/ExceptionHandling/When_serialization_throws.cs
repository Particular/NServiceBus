namespace NServiceBus.AcceptanceTests.ManageFailures
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Xml;
    using Faults;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Config;
    using NServiceBus.MessageMutator;
    using NUnit.Framework;

    public class When_serialization_throws : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_SerializationException()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new Message())))
                    .Done(c => c.ExceptionReceived)
                    .Run();
            Assert.AreEqual(typeof(SerializationException), context.ExceptionType);

            Assert.AreEqual(typeof(XmlException), context.InnerExceptionType);
#if (!DEBUG)
            StackTraceAssert.AreEqual(
@"at NServiceBus.Unicast.Messages.ExtractLogicalMessagesBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Sagas.RemoveIncomingHeadersBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.MessageMutator.ApplyIncomingTransportMessageMutatorsBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.UnitOfWork.UnitOfWorkBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Behaviors.ForwardBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Audit.AuditBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Behaviors.ImpersonateSenderBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Behaviors.MessageHandlingLoggingBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Behaviors.ChildContainerBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Transport.TransportReceiver.ProcessMessage(TransportMessage message)", context.StackTrace);

            StackTraceAssert.AreEqual(
@"at System.Xml.XmlTextReaderImpl.Throw(Exception e)
at System.Xml.XmlTextReaderImpl.ParseQName(Boolean isQName, Int32 startOffset, Int32& colonPos)
at System.Xml.XmlTextReaderImpl.ParseElement()
at System.Xml.XmlTextReaderImpl.ParseDocumentContent()
at System.Xml.XmlLoader.Load(XmlDocument doc, XmlReader reader, Boolean preserveWhitespace)
at System.Xml.XmlDocument.Load(XmlReader reader)
at NServiceBus.Serializers.XML.XmlMessageSerializer.Deserialize(Stream stream, IList`1 messageTypesToDeserialize)
at NServiceBus.Unicast.Messages.ExtractLogicalMessagesBehavior.Extract(TransportMessage physicalMessage)
at NServiceBus.Unicast.Messages.ExtractLogicalMessagesBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)", context.InnerExceptionStackTrace);
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
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Configurer.ConfigureComponent<CustomFaultManager>(DependencyLifecycle.SingleInstance);
                    c.Configurer.ConfigureComponent<CorruptionMutator>(DependencyLifecycle.InstancePerCall);
                    c.DisableTimeoutManager();
                })
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0;
                    })
                    .AllowExceptions();
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
                public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
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