
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
        BehaviorRegistrationsCoordinator coordinator;
        List<RemoveBehavior> removals;
        List<ReplaceBehavior> replacements;

        [SetUp]
        public void Setup()
        {
            removals = new List<RemoveBehavior>();
            replacements = new List<ReplaceBehavior>();

            coordinator = new BehaviorRegistrationsCoordinator(removals, replacements);
        }

        static PipelineStep Step1 = PipelineStep.CreateCustom("1");
        static PipelineStep Step2 = PipelineStep.CreateCustom("2");
        static PipelineStep Step3 = PipelineStep.CreateCustom("3");

        [Test]
        public void Registrations_Count()
        {
            coordinator.Register(Step1, typeof(FakeBehavior), "1");
            coordinator.Register(Step2, typeof(FakeBehavior), "2");
            coordinator.Register(Step3, typeof(FakeBehavior), "3");

            removals.Add(new RemoveBehavior("1"));

            var model = coordinator.BuildRuntimeModel();

            Assert.AreEqual(2, model.Count());
        }

        [Test]
        public void Registrations_Order()
        {
            coordinator.Register(Step1, typeof(FakeBehavior), "1");
            coordinator.Register(Step2, typeof(FakeBehavior), "2");
            coordinator.Register(Step3, typeof(FakeBehavior), "3");

            var model = coordinator.BuildRuntimeModel().ToList();

            Assert.AreEqual("1", model[0].PipelineStep);
            Assert.AreEqual("2", model[1].PipelineStep);
            Assert.AreEqual("3", model[2].PipelineStep);
        }

        [Test]
        public void Registrations_Replace()
        {
            coordinator.Register(Step1, typeof(FakeBehavior), "1");
            coordinator.Register(Step2, typeof(FakeBehavior), "2");
            coordinator.Register(Step3, typeof(FakeBehavior), "3");

            replacements.Add(new ReplaceBehavior("1", typeof(ReplacedBehavior), "new"));
            replacements.Add(new ReplaceBehavior("2", typeof(ReplacedBehavior)));

            var model = coordinator.BuildRuntimeModel().ToList();

            Assert.AreEqual(typeof(ReplacedBehavior).FullName, model[0].BehaviorType.FullName);
            Assert.AreEqual("new", model[0].Description);
            Assert.AreEqual("2", model[1].Description);
        }

        [Test]
        public void Registrations_Order_with_befores_and_afters()
        {
            coordinator.Register(Step1, typeof(FakeBehavior), "1");
            coordinator.Register(Step2, typeof(FakeBehavior), "2");
            coordinator.Register(Step3, typeof(FakeBehavior), "3");

            coordinator.Register(new MyCustomRegistration("1.5", "2", "1"));
            coordinator.Register(new MyCustomRegistration("2.5", "3", "2"));
            coordinator.Register(new MyCustomRegistration("3.5", null, "3"));
            
            var model = coordinator.BuildRuntimeModel().ToList();

            Assert.AreEqual("1", model[0].PipelineStep);
            Assert.AreEqual("1.5", model[1].PipelineStep);
            Assert.AreEqual("2", model[2].PipelineStep);
            Assert.AreEqual("2.5", model[3].PipelineStep);
            Assert.AreEqual("3", model[4].PipelineStep);
            Assert.AreEqual("3.5", model[5].PipelineStep);
        }

        [Test]
        public void Registrations_Order_with_befores_only()
        {
            coordinator.Register(Step1, typeof(FakeBehavior), "1");
            coordinator.Register(Step2, typeof(FakeBehavior), "2");
            coordinator.Register(Step3, typeof(FakeBehavior), "3");

            coordinator.Register(new MyCustomRegistration("1.5", "2,3", null));
            coordinator.Register(new MyCustomRegistration("2.5", "3", null));

            var model = coordinator.BuildRuntimeModel().ToList();

            Assert.AreEqual("1", model[0].PipelineStep);
            Assert.AreEqual("1.5", model[1].PipelineStep);
            Assert.AreEqual("2", model[2].PipelineStep);
            Assert.AreEqual("2.5", model[3].PipelineStep);
            Assert.AreEqual("3", model[4].PipelineStep);
        }

        [Test]
        public void Registrations_Order_with_multi_afters()
        {
            coordinator.Register(Step1, typeof(FakeBehavior), "1");
            coordinator.Register(Step2, typeof(FakeBehavior), "2");
            coordinator.Register(Step3, typeof(FakeBehavior), "3");

            coordinator.Register(new MyCustomRegistration("1.5", "2", "1"));
            coordinator.Register(new MyCustomRegistration("2.5", "3", "2,1"));
            coordinator.Register(new MyCustomRegistration("3.5", null, "1,2,3"));

            var model = coordinator.BuildRuntimeModel().ToList();

            Assert.AreEqual("1", model[0].PipelineStep);
            Assert.AreEqual("1.5", model[1].PipelineStep);
            Assert.AreEqual("2", model[2].PipelineStep);
            Assert.AreEqual("2.5", model[3].PipelineStep);
            Assert.AreEqual("3", model[4].PipelineStep);
            Assert.AreEqual("3.5", model[5].PipelineStep);
        }

        [Test]
        public void Registrations_Order_with_afters_only()
        {
            coordinator.Register(Step1, typeof(FakeBehavior), "1");
            coordinator.Register(Step2, typeof(FakeBehavior), "2");
            coordinator.Register(Step3, typeof(FakeBehavior), "3");

            coordinator.Register(new MyCustomRegistration("1.5", "1.6", "1.1"));
            coordinator.Register(new MyCustomRegistration("1.6", "2", "1.5"));
            coordinator.Register(new MyCustomRegistration("1.1", "1.5", "1"));

            var model = coordinator.BuildRuntimeModel().ToList();

            Assert.AreEqual("1", model[0].PipelineStep);
            Assert.AreEqual("1.1", model[1].PipelineStep);
            Assert.AreEqual("1.5", model[2].PipelineStep);
            Assert.AreEqual("1.6", model[3].PipelineStep);
            Assert.AreEqual("2", model[4].PipelineStep);
            Assert.AreEqual("3", model[5].PipelineStep);
        }

        class MyCustomRegistration : RegisterBehavior
        {
            public MyCustomRegistration(string pipelineStep, string before, string after)
                : base(NServiceBus.Pipeline.PipelineStep.CreateCustom(pipelineStep), typeof(FakeBehavior), pipelineStep)
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
        class FakeBehavior:IBehavior<IncomingContext>
        {
            public void Invoke(IncomingContext context, Action next)
            {
                throw new NotImplementedException();
            }
        }

        class ReplacedBehavior : IBehavior<IncomingContext>
        {
            public void Invoke(IncomingContext context, Action next)
            {
                throw new NotImplementedException();
            }
        }
    }
}
