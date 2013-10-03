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
            funcBuilder.Register<RavenDbUnitOfWorkBehavior>(() => new RavenDbUnitOfWorkBehavior(funcBuilder));
            funcBuilder.Register<OrdinaryMessageHandlerDispatcherBehavior>(() => new OrdinaryMessageHandlerDispatcherBehavior());
            documentStore = MockRepository.GenerateMock<IDocumentStore>();
            documentStore.Stub(s => s.OpenSession()).Return(MockRepository.GenerateMock<IDocumentSession>());
            funcBuilder.Register<IDocumentStore>(() => documentStore);

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
            readonly IBuilder builder;

            public RavenDbUnitOfWorkBehavior(IBuilder builder)
            {
                this.builder = builder;
            }

            public IBehavior Next { get; set; }

            public void Invoke(IBehaviorContext context)
            {
                var documentStore = builder.Build<IDocumentStore>();

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