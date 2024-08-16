namespace NServiceBus.Core.Tests.Pipeline;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using NUnit.Framework;

[TestFixture]
class BehaviorRegistrationsCoordinatorTests
{
    StepRegistrationsCoordinator coordinator;
    List<ReplaceStep> replacements;
    List<RegisterOrReplaceStep> addOrReplacements;

    [SetUp]
    public void Setup()
    {
        replacements = [];
        addOrReplacements = [];

        coordinator = new StepRegistrationsCoordinator(replacements, addOrReplacements);
    }

    [Test]
    public void Registrations_Count()
    {
        coordinator.Register("1", typeof(FakeBehavior), "1");
        coordinator.Register("2", typeof(FakeBehavior), "2");
        coordinator.Register("3", typeof(FakeBehavior), "3");

        var model = coordinator.BuildPipelineModelFor<IRootContext>();

        Assert.That(model.Count, Is.EqualTo(3));
    }

    [Test]
    public void Registrations_Order()
    {
        coordinator.Register("1", typeof(FakeBehavior), "1");
        coordinator.Register("2", typeof(FakeBehavior), "2");
        coordinator.Register("3", typeof(FakeBehavior), "3");

        var model = coordinator.BuildPipelineModelFor<IRootContext>().ToList();

        Assert.That(model[0].StepId, Is.EqualTo("1"));
        Assert.That(model[1].StepId, Is.EqualTo("2"));
        Assert.That(model[2].StepId, Is.EqualTo("3"));
    }

    [Test]
    public void Registrations_Replace()
    {
        coordinator.Register("1", typeof(FakeBehavior), "1");
        coordinator.Register("2", typeof(FakeBehavior), "2");
        coordinator.Register("3", typeof(FakeBehavior), "3");

        replacements.Add(new ReplaceStep("1", typeof(ReplacedBehavior), "new"));
        replacements.Add(new ReplaceStep("2", typeof(ReplacedBehavior)));

        var model = coordinator.BuildPipelineModelFor<IRootContext>().ToList();

        Assert.That(model[0].BehaviorType.FullName, Is.EqualTo(typeof(ReplacedBehavior).FullName));
        Assert.That(model[0].Description, Is.EqualTo("new"));
        Assert.That(model[1].Description, Is.EqualTo("2"));
    }

    [Test]
    public void Registrations_AddOrReplace_WhenDoesNotExist()
    {
        addOrReplacements.Add(RegisterOrReplaceStep.Create("1", typeof(ReplacedBehavior), "new"));

        var model = coordinator.BuildPipelineModelFor<IRootContext>().ToList();

        Assert.That(model.Count, Is.EqualTo(1));
        Assert.That(model[0].BehaviorType.FullName, Is.EqualTo(typeof(ReplacedBehavior).FullName));
        Assert.That(model[0].Description, Is.EqualTo("new"));
    }

    [Test]
    public void Registrations_AddOrReplace_WhenExists()
    {
        coordinator.Register("1", typeof(FakeBehavior), "1");

        addOrReplacements.Add(RegisterOrReplaceStep.Create("1", typeof(ReplacedBehavior), "new"));

        var model = coordinator.BuildPipelineModelFor<IRootContext>().ToList();

        Assert.That(model.Count, Is.EqualTo(1));
        Assert.That(model[0].BehaviorType.FullName, Is.EqualTo(typeof(ReplacedBehavior).FullName));
        Assert.That(model[0].Description, Is.EqualTo("new"));
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

        var model = coordinator.BuildPipelineModelFor<IRootContext>().ToList();

        Assert.That(model[0].StepId, Is.EqualTo("1"));
        Assert.That(model[1].StepId, Is.EqualTo("1.5"));
        Assert.That(model[2].StepId, Is.EqualTo("2"));
        Assert.That(model[3].StepId, Is.EqualTo("2.5"));
        Assert.That(model[4].StepId, Is.EqualTo("3"));
        Assert.That(model[5].StepId, Is.EqualTo("3.5"));
    }

    [Test]
    public void Registrations_Order_with_befores_only()
    {
        coordinator.Register("1", typeof(FakeBehavior), "1");
        coordinator.Register("2", typeof(FakeBehavior), "2");
        coordinator.Register("3", typeof(FakeBehavior), "3");

        coordinator.Register(new MyCustomRegistration("1.5", "2,3", null));
        coordinator.Register(new MyCustomRegistration("2.5", "3", null));

        var model = coordinator.BuildPipelineModelFor<IRootContext>().ToList();

        Assert.That(model[0].StepId, Is.EqualTo("1"));
        Assert.That(model[1].StepId, Is.EqualTo("1.5"));
        Assert.That(model[2].StepId, Is.EqualTo("2"));
        Assert.That(model[3].StepId, Is.EqualTo("2.5"));
        Assert.That(model[4].StepId, Is.EqualTo("3"));
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

        var model = coordinator.BuildPipelineModelFor<IRootContext>().ToList();

        Assert.That(model[0].StepId, Is.EqualTo("1"));
        Assert.That(model[1].StepId, Is.EqualTo("1.5"));
        Assert.That(model[2].StepId, Is.EqualTo("2"));
        Assert.That(model[3].StepId, Is.EqualTo("2.5"));
        Assert.That(model[4].StepId, Is.EqualTo("3"));
        Assert.That(model[5].StepId, Is.EqualTo("3.5"));
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

        var model = coordinator.BuildPipelineModelFor<IRootContext>().ToList();

        Assert.That(model[0].StepId, Is.EqualTo("1"));
        Assert.That(model[1].StepId, Is.EqualTo("1.1"));
        Assert.That(model[2].StepId, Is.EqualTo("1.5"));
        Assert.That(model[3].StepId, Is.EqualTo("1.6"));
        Assert.That(model[4].StepId, Is.EqualTo("2"));
        Assert.That(model[5].StepId, Is.EqualTo("3"));
    }

    [Test]
    public void Should_throw_if_behavior_wants_to_go_before_connector()
    {
        coordinator.Register("connector", typeof(FakeStageConnector), "Connector");

        coordinator.Register(new MyCustomRegistration("x", "connector", ""));


        Assert.Throws<Exception>(() => coordinator.BuildPipelineModelFor<IRootContext>());
    }

    [Test]
    public void Should_throw_if_behavior_wants_to_go_after_connector()
    {
        coordinator.Register("connector", typeof(FakeStageConnector), "Connector");

        coordinator.Register(new MyCustomRegistration("x", "", "connector"));


        Assert.Throws<Exception>(() => coordinator.BuildPipelineModelFor<IRootContext>());
    }

    class MyCustomRegistration : RegisterStep
    {
        public MyCustomRegistration(string pipelineStep, string before, string after)
            : base(pipelineStep, typeof(FakeBehavior), pipelineStep)
        {
            if (!string.IsNullOrEmpty(before))
            {
                foreach (var b in before.Split(','))
                {
                    InsertBefore(b);
                }
            }

            if (!string.IsNullOrEmpty(after))
            {
                foreach (var a in after.Split(','))
                {
                    InsertAfter(a);

                }
            }
        }
    }

    class FakeBehavior : IBehavior<IRootContext, IRootContext>
    {
        public Task Invoke(IRootContext context, Func<IRootContext, Task> next)
        {
            throw new NotImplementedException();
        }
    }


    class ReplacedBehavior : IBehavior<IRootContext, IRootContext>
    {
        public Task Invoke(IRootContext context, Func<IRootContext, Task> next)
        {
            throw new NotImplementedException();
        }
    }

    class FakeStageConnector : StageConnector<IRootContext, IChildContext>
    {
        public override Task Invoke(IRootContext context, Func<IChildContext, Task> stage)
        {
            throw new NotImplementedException();
        }
    }

    interface IRootContext : IBehaviorContext { }

    interface IChildContext : IIncomingContext { }

    class ChildContext : IncomingContext, IChildContext
    {
        public ChildContext() : base("messageId", "replyToAddress", new Dictionary<string, string>(), null)
        {
        }
    }
}
