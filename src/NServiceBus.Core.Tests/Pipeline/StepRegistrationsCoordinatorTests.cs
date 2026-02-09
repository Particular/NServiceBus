namespace NServiceBus.Core.Tests.Pipeline;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using NUnit.Framework;

[TestFixture]
class StepRegistrationsCoordinatorTests
{
    StepRegistrationsCoordinator coordinator;
    List<RegisterStep> additions;
    List<ReplaceStep> replacements;
    List<RegisterOrReplaceStep> addOrReplacements;

    [SetUp]
    public void Setup()
    {
        additions = [];
        replacements = [];
        addOrReplacements = [];

        coordinator = new StepRegistrationsCoordinator(additions, replacements, addOrReplacements);
    }

    [Test]
    public void Registrations_Count()
    {
        additions.Add(RegisterStep.Create("1", typeof(FakeBehavior), "1"));
        additions.Add(RegisterStep.Create("2", typeof(FakeBehavior), "2"));
        additions.Add(RegisterStep.Create("3", typeof(FakeBehavior), "3"));

        var model = coordinator.BuildPipelineBuildModelFor<IRootContext>().Steps;

        Assert.That(model.Count, Is.EqualTo(3));
    }

    [Test]
    public void Registrations_Order()
    {
        additions.Add(RegisterStep.Create("1", typeof(FakeBehavior), "1"));
        additions.Add(RegisterStep.Create("2", typeof(FakeBehavior), "2"));
        additions.Add(RegisterStep.Create("3", typeof(FakeBehavior), "3"));

        var model = coordinator.BuildPipelineBuildModelFor<IRootContext>().Steps;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model[0].StepId, Is.EqualTo("1"));
            Assert.That(model[1].StepId, Is.EqualTo("2"));
            Assert.That(model[2].StepId, Is.EqualTo("3"));
        }
    }

    [Test]
    public void Registrations_Replace()
    {
        additions.Add(RegisterStep.Create("1", typeof(FakeBehavior), "1"));
        additions.Add(RegisterStep.Create("2", typeof(FakeBehavior), "2"));
        additions.Add(RegisterStep.Create("3", typeof(FakeBehavior), "3"));

        replacements.Add(new ReplaceStep("1", typeof(ReplacedBehavior), "new"));
        replacements.Add(new ReplaceStep("2", typeof(ReplacedBehavior)));

        var model = coordinator.BuildPipelineBuildModelFor<IRootContext>().Steps;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model[0].BehaviorType.FullName, Is.EqualTo(typeof(ReplacedBehavior).FullName));
            Assert.That(model[0].Description, Is.EqualTo("new"));
            Assert.That(model[1].Description, Is.EqualTo("2"));
        }
    }

    [Test]
    public void Registrations_AddOrReplace_WhenDoesNotExist()
    {
        addOrReplacements.Add(RegisterOrReplaceStep.Create("1", typeof(ReplacedBehavior), "new"));

        var model = coordinator.BuildPipelineBuildModelFor<IRootContext>().Steps;

        Assert.That(model.Count, Is.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(model[0].BehaviorType.FullName, Is.EqualTo(typeof(ReplacedBehavior).FullName));
            Assert.That(model[0].Description, Is.EqualTo("new"));
        }
    }

    [Test]
    public void Registrations_AddOrReplace_WhenExists()
    {
        additions.Add(RegisterStep.Create("1", typeof(FakeBehavior), "1"));

        addOrReplacements.Add(RegisterOrReplaceStep.Create("1", typeof(ReplacedBehavior), "new"));

        var model = coordinator.BuildPipelineBuildModelFor<IRootContext>().Steps;

        Assert.That(model.Count, Is.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(model[0].BehaviorType.FullName, Is.EqualTo(typeof(ReplacedBehavior).FullName));
            Assert.That(model[0].Description, Is.EqualTo("new"));
        }
    }

    [Test]
    public void Registrations_Order_with_befores_and_afters()
    {
        additions.Add(RegisterStep.Create("1", typeof(FakeBehavior), "1"));
        additions.Add(RegisterStep.Create("2", typeof(FakeBehavior), "2"));
        additions.Add(RegisterStep.Create("3", typeof(FakeBehavior), "3"));

        additions.Add(new MyCustomRegistration("1.5", "2", "1"));
        additions.Add(new MyCustomRegistration("2.5", "3", "2"));
        additions.Add(new MyCustomRegistration("3.5", null, "3"));

        var model = coordinator.BuildPipelineBuildModelFor<IRootContext>().Steps;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model[0].StepId, Is.EqualTo("1"));
            Assert.That(model[1].StepId, Is.EqualTo("1.5"));
            Assert.That(model[2].StepId, Is.EqualTo("2"));
            Assert.That(model[3].StepId, Is.EqualTo("2.5"));
            Assert.That(model[4].StepId, Is.EqualTo("3"));
            Assert.That(model[5].StepId, Is.EqualTo("3.5"));
        }
    }

    [Test]
    public void Registrations_Order_with_befores_only()
    {
        additions.Add(RegisterStep.Create("1", typeof(FakeBehavior), "1"));
        additions.Add(RegisterStep.Create("2", typeof(FakeBehavior), "2"));
        additions.Add(RegisterStep.Create("3", typeof(FakeBehavior), "3"));

        additions.Add(new MyCustomRegistration("1.5", "2,3", null));
        additions.Add(new MyCustomRegistration("2.5", "3", null));

        var model = coordinator.BuildPipelineBuildModelFor<IRootContext>().Steps;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model[0].StepId, Is.EqualTo("1"));
            Assert.That(model[1].StepId, Is.EqualTo("1.5"));
            Assert.That(model[2].StepId, Is.EqualTo("2"));
            Assert.That(model[3].StepId, Is.EqualTo("2.5"));
            Assert.That(model[4].StepId, Is.EqualTo("3"));
        }
    }

    [Test]
    public void Registrations_Order_with_multi_afters()
    {
        additions.Add(RegisterStep.Create("1", typeof(FakeBehavior), "1"));
        additions.Add(RegisterStep.Create("2", typeof(FakeBehavior), "2"));
        additions.Add(RegisterStep.Create("3", typeof(FakeBehavior), "3"));

        additions.Add(new MyCustomRegistration("1.5", "2", "1"));
        additions.Add(new MyCustomRegistration("2.5", "3", "2,1"));
        additions.Add(new MyCustomRegistration("3.5", null, "1,2,3"));

        var model = coordinator.BuildPipelineBuildModelFor<IRootContext>().Steps;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model[0].StepId, Is.EqualTo("1"));
            Assert.That(model[1].StepId, Is.EqualTo("1.5"));
            Assert.That(model[2].StepId, Is.EqualTo("2"));
            Assert.That(model[3].StepId, Is.EqualTo("2.5"));
            Assert.That(model[4].StepId, Is.EqualTo("3"));
            Assert.That(model[5].StepId, Is.EqualTo("3.5"));
        }
    }

    [Test]
    public void Registrations_Order_with_afters_only()
    {
        additions.Add(RegisterStep.Create("1", typeof(FakeBehavior), "1"));
        additions.Add(RegisterStep.Create("2", typeof(FakeBehavior), "2"));
        additions.Add(RegisterStep.Create("3", typeof(FakeBehavior), "3"));

        additions.Add(new MyCustomRegistration("1.5", "1.6", "1.1"));
        additions.Add(new MyCustomRegistration("1.6", "2", "1.5"));
        additions.Add(new MyCustomRegistration("1.1", "1.5", "1"));

        var model = coordinator.BuildPipelineBuildModelFor<IRootContext>().Steps;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model[0].StepId, Is.EqualTo("1"));
            Assert.That(model[1].StepId, Is.EqualTo("1.1"));
            Assert.That(model[2].StepId, Is.EqualTo("1.5"));
            Assert.That(model[3].StepId, Is.EqualTo("1.6"));
            Assert.That(model[4].StepId, Is.EqualTo("2"));
            Assert.That(model[5].StepId, Is.EqualTo("3"));
        }
    }

    [Test]
    public void Should_throw_if_behavior_wants_to_go_before_connector()
    {
        additions.Add(RegisterStep.Create("connector", typeof(FakeStageConnector), "Connector"));

        additions.Add(new MyCustomRegistration("x", "connector", ""));

        Assert.Throws<Exception>(() => coordinator.BuildPipelineBuildModelFor<IRootContext>());
    }

    [Test]
    public void Should_throw_if_behavior_wants_to_go_after_connector()
    {
        additions.Add(RegisterStep.Create("connector", typeof(FakeStageConnector), "Connector"));

        additions.Add(new MyCustomRegistration("x", "", "connector"));

        Assert.Throws<Exception>(() => coordinator.BuildPipelineBuildModelFor<IRootContext>());
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
