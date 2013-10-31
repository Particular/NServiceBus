//namespace NServiceBus.Core.Tests.Pipeline
//{
//    using System;
//    using System.Reflection;
//    using NServiceBus.Pipeline;
//    using NUnit.Framework;
//    using ObjectBuilder;
//    using Raven.Client;
//    using Rhino.Mocks;

//    [TestFixture]
//    public class MessAround
//    {
//        FuncBuilder funcBuilder;
//        IDocumentStore documentStore;

//        [SetUp]
//        public void SetUp()
//        {
//            Configure.With(new Assembly[0])
//                     .DefineEndpointName("Test")
//                     .DefaultBuilder();
            
//            funcBuilder = new FuncBuilder();
//        }

//        [Test]
//        public void Sample_unit_of_work_behavior_that_leans_on_the_container_to_transfer_objects_around()
//        {
//            documentStore = MockRepository.GenerateMock<IDocumentStore>();
//            documentStore.Stub(s => s.OpenSession()).Return(MockRepository.GenerateMock<IDocumentSession>());

//            //in real life this would be a Configure.Component<IDocumentSession>(b=>b.Build<IDocumentStore>().OpenSession())
//            var stubSession = MockRepository.GenerateMock<IDocumentSession>();
            
//            funcBuilder.Register<RavenDbUnitOfWorkBehavior>(() => new RavenDbUnitOfWorkBehavior{Builder = funcBuilder});
//            funcBuilder.Register<OrdinaryMessageHandlerDispatcherBehavior>(() => new OrdinaryMessageHandlerDispatcherBehavior{Builder = funcBuilder});
//            funcBuilder.Register<IDocumentSession>(() => stubSession);
//            funcBuilder.Register<MyMessageThatStoresDataInRavenHandler>(() => new MyMessageThatStoresDataInRavenHandler());

//            var pipeline = new BehaviorChain(() => funcBuilder);

//            pipeline.Add<RavenDbUnitOfWorkBehavior>();
//            pipeline.Add<OrdinaryMessageHandlerDispatcherBehavior>();

//            var incomingTransportMessage = new TransportMessage();

//            pipeline.Invoke(incomingTransportMessage);
//        }

//        //this is a behaviour that "might" be created be a end user
//        public class RavenDbUnitOfWorkBehavior : IBehavior
//        {
//            public IBuilder Builder { get; set; }

//            public void Invoke(IBehaviorContext context)
//            {
//                using (var session = Builder.Build<IDocumentSession>())
//                {
//                    context.Trace("Session #{0}", session.GetHashCode());
//                    Console.Out.WriteLine("Session: " + session.GetHashCode());

//                    //context.Set(session);  Not needed since any one (is this case users) would just take a dep on IDocumentSession
//                    // this works for other behaviours as well but I'd argue that we should stay away from the container as much as possible
//                    //for our internal stuff
//                    Next.Invoke(context);
                    
//                    context.Trace("Saving changes");
//                    session.SaveChanges();
//                }
//            }
//        }

//        public class MyMessageThatStoresDataInRavenHandler: IHandleMessages<MyMessageThatStoresDataInRaven>
//        {
//            public IDocumentSession Session { get; set; }

//            public void Handle(MyMessageThatStoresDataInRaven message)
//            {
//                Console.Out.WriteLine("Session: " + Session.GetHashCode());
//                Session.Store(new SomeDocument());
//            }
//        }

//        public class SomeDocument
//        {
//        }

//        public  class MyMessageThatStoresDataInRaven
//        {
//        }


//        public class OrdinaryMessageHandlerDispatcherBehavior : IBehavior
//        {
//            public IBehavior Next { get; set; }

//            public IBuilder Builder { get; set; }

//            public void Invoke(IBehaviorContext context)
//            {
//                context.Trace("Resolving handler");
//                //hardcoded
//                var handler = Builder.Build<MyMessageThatStoresDataInRavenHandler>();

//                context.Trace("Dispatching to {0}", handler);
//                //handler.Handle((MyMessageThatStoresDataInRaven)context.Message);
//                handler.Handle(null); //just for now
//            }
//        }
//    }

//}