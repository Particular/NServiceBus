namespace NServiceBus.Core.Tests.Pipeline
{
    using System.Reflection;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using ObjectBuilder;
    using Raven.Client;
    using Rhino.Mocks;

    [TestFixture]
    public class MessAround
    {
        FuncBuilder funcBuilder;
        IDocumentStore documentStore;

        [SetUp]
        public void SetUp()
        {
            Configure.With(new Assembly[0])
                     .DefineEndpointName("Test")
                     .DefaultBuilder();
            
            funcBuilder = new FuncBuilder();
            
            Configure.Instance.Builder = funcBuilder;
        }

        [Test]
        public void JustDoit()
        {
            documentStore = MockRepository.GenerateMock<IDocumentStore>();
            documentStore.Stub(s => s.OpenSession()).Return(MockRepository.GenerateMock<IDocumentSession>());

            funcBuilder.Register<RavenDbUnitOfWorkBehavior>(() => new RavenDbUnitOfWorkBehavior(documentStore));
            funcBuilder.Register<OrdinaryMessageHandlerDispatcherBehavior>(() => new OrdinaryMessageHandlerDispatcherBehavior());

            var pipeline =
                new BehaviorChain
                    {
                        typeof(RavenDbUnitOfWorkBehavior),
                        typeof(OrdinaryMessageHandlerDispatcherBehavior)
                    };

            var incomingTransportMessage = new TransportMessage();

            pipeline.Invoke(incomingTransportMessage);
        }


        public class RavenDbUnitOfWorkBehavior : IBehavior
        {
            readonly IDocumentStore documentStore;

            public RavenDbUnitOfWorkBehavior(IDocumentStore documentStore)
            {
                this.documentStore = documentStore;
            }

            public IBehavior Next { get; set; }

            public void Invoke(IBehaviorContext context)
            {
                using (var session = documentStore.OpenSession())
                {
                    context.Set(session);
                    Next.Invoke(context);
                    session.SaveChanges();
                }
            }
        }


        public class OrdinaryMessageHandlerDispatcherBehavior : IBehavior
        {
            public IBehavior Next { get; set; }

            public void Invoke(IBehaviorContext context)
            {
            }
        }
    }
}