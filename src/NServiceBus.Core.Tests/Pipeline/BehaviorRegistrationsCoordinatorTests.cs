
namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;

    [TestFixture]
    class BehaviorRegistrationsCoordinatorTests
    {
        StepRegistrationsCoordinator coordinator;
        List<RemoveStep> removals;
        List<ReplaceBehavior> replacements;

        [SetUp]
        public void Setup()
        {
            removals = new List<RemoveStep>();
            replacements = new List<ReplaceBehavior>();

            coordinator = new StepRegistrationsCoordinator(removals, replacements);
        }

        [Test]
        public void Registrations_Count()
        {
            coordinator.Register("1", typeof(FakeBehavior), "1");
            coordinator.Register("2", typeof(FakeBehavior), "2");
            coordinator.Register("3", typeof(FakeBehavior), "3");

            removals.Add(new RemoveStep("1"));

            var model = coordinator.BuildPipelineModelFor<IncomingContext>();

            Assert.AreEqual(2, model.Count());
        }

        [Test]
        public void Registrations_Order()
        {
            coordinator.Register("1", typeof(FakeBehavior), "1");
            coordinator.Register("2", typeof(FakeBehavior), "2");
            coordinator.Register("3", typeof(FakeBehavior), "3");

            var model =  coordinator.BuildPipelineModelFor<IncomingContext>().ToList();

            Assert.AreEqual("1", model[0].StepId);
            Assert.AreEqual("2", model[1].StepId);
            Assert.AreEqual("3", model[2].StepId);
        }

        [Test]
        public void Registrations_Replace()
        {
            coordinator.Register("1", typeof(FakeBehavior), "1");
            coordinator.Register("2", typeof(FakeBehavior), "2");
            coordinator.Register("3", typeof(FakeBehavior), "3");

            replacements.Add(new ReplaceBehavior("1", typeof(ReplacedBehavior), "new"));
            replacements.Add(new ReplaceBehavior("2", typeof(ReplacedBehavior)));

            var model = coordinator.BuildPipelineModelFor<IncomingContext>().ToList();

            Assert.AreEqual(typeof(ReplacedBehavior).FullName, model[0].BehaviorType.FullName);
            Assert.AreEqual("new", model[0].Description);
            Assert.AreEqual("2", model[1].Description);
        }

        [Test]
        public void Registrations_Order_with_befores_and_afters()
        {
            coordinator.Register("1", typeof(FakeBehavior), "1");
            coordinator.Register("2", typeof(FakeBehavior), "2");
            coordinator.Register("3", typeof(FakeBehavior), "3");

            coordinator.Register(new MyCustomRegistration("1.5", "2", "1"));
            coordinator.Register(new MyCustomRegistration("2.5", "3", "2"));
            coordinator.Register(new MyCustomRegistration("3.5", null, "3"));

            var model = coordinator.BuildPipelineModelFor<IncomingContext>().ToList();

            Assert.AreEqual("1", model[0].StepId);
            Assert.AreEqual("1.5", model[1].StepId);
            Assert.AreEqual("2", model[2].StepId);
            Assert.AreEqual("2.5", model[3].StepId);
            Assert.AreEqual("3", model[4].StepId);
            Assert.AreEqual("3.5", model[5].StepId);
        }

        [Test]
        public void Registrations_Order_with_befores_only()
        {
            coordinator.Register("1", typeof(FakeBehavior), "1");
            coordinator.Register("2", typeof(FakeBehavior), "2");
            coordinator.Register("3", typeof(FakeBehavior), "3");

            coordinator.Register(new MyCustomRegistration("1.5", "2,3", null));
            coordinator.Register(new MyCustomRegistration("2.5", "3", null));

            var model = coordinator.BuildPipelineModelFor<IncomingContext>().ToList();

            Assert.AreEqual("1", model[0].StepId);
            Assert.AreEqual("1.5", model[1].StepId);
            Assert.AreEqual("2", model[2].StepId);
            Assert.AreEqual("2.5", model[3].StepId);
            Assert.AreEqual("3", model[4].StepId);
        }

        [Test]
        public void Registrations_Order_with_multi_afters()
        {
            coordinator.Register("1", typeof(FakeBehavior), "1");
            coordinator.Register("2", typeof(FakeBehavior), "2");
            coordinator.Register("3", typeof(FakeBehavior), "3");

            coordinator.Register(new MyCustomRegistration("1.5", "2", "1"));
            coordinator.Register(new MyCustomRegistration("2.5", "3", "2,1"));
            coordinator.Register(new MyCustomRegistration("3.5", null, "1,2,3"));

            var model = coordinator.BuildPipelineModelFor<IncomingContext>().ToList();

            Assert.AreEqual("1", model[0].StepId);
            Assert.AreEqual("1.5", model[1].StepId);
            Assert.AreEqual("2", model[2].StepId);
            Assert.AreEqual("2.5", model[3].StepId);
            Assert.AreEqual("3", model[4].StepId);
            Assert.AreEqual("3.5", model[5].StepId);
        }

        [Test]
        public void Registrations_Order_with_afters_only()
        {
            coordinator.Register("1", typeof(FakeBehavior), "1");
            coordinator.Register("2", typeof(FakeBehavior), "2");
            coordinator.Register("3", typeof(FakeBehavior), "3");

            coordinator.Register(new MyCustomRegistration("1.5", "1.6", "1.1"));
            coordinator.Register(new MyCustomRegistration("1.6", "2", "1.5"));
            coordinator.Register(new MyCustomRegistration("1.1", "1.5", "1"));

            var model = coordinator.BuildPipelineModelFor<IncomingContext>().ToList();

            Assert.AreEqual("1", model[0].StepId);
            Assert.AreEqual("1.1", model[1].StepId);
            Assert.AreEqual("1.5", model[2].StepId);
            Assert.AreEqual("1.6", model[3].StepId);
            Assert.AreEqual("2", model[4].StepId);
            Assert.AreEqual("3", model[5].StepId);
        }

        [Test]
        public void Should_throw_if_behavior_wants_to_go_before_connector()
        {
            coordinator.Register("connector", typeof(FakeStageConnector), "Connector");

            coordinator.Register(new MyCustomRegistration("x","connector",""));


            Assert.Throws<Exception>(() => coordinator.BuildPipelineModelFor<IncomingContext>());
        }

        [Test]
        public void Should_throw_if_behavior_wants_to_go_after_connector()
        {
            coordinator.Register("connector", typeof(FakeStageConnector), "Connector");

            coordinator.Register(new MyCustomRegistration("x", "", "connector"));


            Assert.Throws<Exception>(() => coordinator.BuildPipelineModelFor<IncomingContext>());
        }

        [Test]
        public void Show_detect_missing_stage_connectors()
        {
            coordinator.Register("connector", typeof(FakeStageConnector), "Connector");

            coordinator.Register("fake", typeof(FakeBehavior), "x");
            coordinator.Register("childfake", typeof(ChildFakeBehavior), "x"); 
            coordinator.Register("child2fake", typeof(Child2FakeBehavior), "x");

            Assert.Throws<Exception>(() => coordinator.BuildPipelineModelFor<IncomingContext>());
        }

        class MyCustomRegistration : RegisterStep
        {
            public MyCustomRegistration(string pipelineStep, string before, string after)
                : base(pipelineStep, typeof(FakeBehavior), pipelineStep)
            {
                if (!String.IsNullOrEmpty(before))
                {
                    foreach (var b in before.Split(','))
                    {
                        InsertBefore(b);
                        
                    }
                }

                if (!String.IsNullOrEmpty(after))
                {
                    foreach (var a in after.Split(','))
                    {
                        InsertAfter(a);

                    }
                }
            }
        }
        class FakeBehavior: Behavior<IncomingContext>
        {
            public override void Invoke(IncomingContext context, Action next)
            {
                throw new NotImplementedException();
            }
        }

        class ChildFakeBehavior : Behavior<ChildContext>
        {
            public override void Invoke(ChildContext context, Action next)
            {
                throw new NotImplementedException();
            }
        }
        class Child2FakeBehavior : Behavior<Child2Context>
        {
            public override void Invoke(Child2Context context, Action next)
            {
                throw new NotImplementedException();
            }
        }

        class ReplacedBehavior : Behavior<IncomingContext>
        {
            public override void Invoke(IncomingContext context, Action next)
            {
                throw new NotImplementedException();
            }
        }

        class FakeStageConnector : StageConnector<IncomingContext,ChildContext>
        {
            public override void Invoke(IncomingContext context, Action<ChildContext> next)
            {
                throw new NotImplementedException();
            }
        }

        class ChildContext : IncomingContext
        {
            public ChildContext() : base(null)
            {
            }
        }

        class Child2Context : ChildContext
        {
        }

     
    }
}
