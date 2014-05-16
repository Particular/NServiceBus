
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

        [Test]
        public void Registrations_Count()
        {
            coordinator.Register("1", typeof(FakeBehavior), "1");
            coordinator.Register("2", typeof(FakeBehavior), "2");
            coordinator.Register("3", typeof(FakeBehavior), "3");

            removals.Add(new RemoveBehavior("1"));

            var model = coordinator.BuildRuntimeModel();

            Assert.AreEqual(2, model.Count());
        }

        [Test]
        public void Registrations_Order()
        {
            coordinator.Register("1", typeof(FakeBehavior), "1");
            coordinator.Register("2", typeof(FakeBehavior), "2");
            coordinator.Register("3", typeof(FakeBehavior), "3");

            var model = coordinator.BuildRuntimeModel().ToList();

            Assert.AreEqual("1", model[0].Id);
            Assert.AreEqual("2", model[1].Id);
            Assert.AreEqual("3", model[2].Id);
        }

        [Test]
        public void Registrations_Replace()
        {
            coordinator.Register("1", typeof(FakeBehavior), "1");
            coordinator.Register("2", typeof(FakeBehavior), "2");
            coordinator.Register("3", typeof(FakeBehavior), "3");

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
            coordinator.Register("1", typeof(FakeBehavior), "1");
            coordinator.Register("2", typeof(FakeBehavior), "2");
            coordinator.Register("3", typeof(FakeBehavior), "3");

            coordinator.Register(new MyCustomRegistration("1.5", "2", "1"));
            coordinator.Register(new MyCustomRegistration("2.5", "3", "2"));
            coordinator.Register(new MyCustomRegistration("3.5", null, "3"));
            
            var model = coordinator.BuildRuntimeModel().ToList();

            Assert.AreEqual("1", model[0].Id);
            Assert.AreEqual("1.5", model[1].Id);
            Assert.AreEqual("2", model[2].Id);
            Assert.AreEqual("2.5", model[3].Id);
            Assert.AreEqual("3", model[4].Id);
            Assert.AreEqual("3.5", model[5].Id);
        }

        [Test]
        public void Registrations_Order_with_befores_only()
        {
            coordinator.Register("1", typeof(FakeBehavior), "1");
            coordinator.Register("2", typeof(FakeBehavior), "2");
            coordinator.Register("3", typeof(FakeBehavior), "3");

            coordinator.Register(new MyCustomRegistration("1.5", "2,3", null));
            coordinator.Register(new MyCustomRegistration("2.5", "3", null));

            var model = coordinator.BuildRuntimeModel().ToList();

            Assert.AreEqual("1", model[0].Id);
            Assert.AreEqual("1.5", model[1].Id);
            Assert.AreEqual("2", model[2].Id);
            Assert.AreEqual("2.5", model[3].Id);
            Assert.AreEqual("3", model[4].Id);
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

            var model = coordinator.BuildRuntimeModel().ToList();

            Assert.AreEqual("1", model[0].Id);
            Assert.AreEqual("1.5", model[1].Id);
            Assert.AreEqual("2", model[2].Id);
            Assert.AreEqual("2.5", model[3].Id);
            Assert.AreEqual("3", model[4].Id);
            Assert.AreEqual("3.5", model[5].Id);
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

            var model = coordinator.BuildRuntimeModel().ToList();

            Assert.AreEqual("1", model[0].Id);
            Assert.AreEqual("1.1", model[1].Id);
            Assert.AreEqual("1.5", model[2].Id);
            Assert.AreEqual("1.6", model[3].Id);
            Assert.AreEqual("2", model[4].Id);
            Assert.AreEqual("3", model[5].Id);
        }

        class MyCustomRegistration : RegisterBehavior
        {
            public MyCustomRegistration(string id, string before, string after)
                : base(id, typeof(FakeBehavior), id)
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
