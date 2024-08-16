namespace NServiceBus.Core.Tests.Pipeline;

using System;
using System.Threading.Tasks;
using Features;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class RegisterStepTests
{
    [Test]
    public void Replace_WhenStepIdsDoNotMatch_ShouldThrowInvalidOperationException()
    {
        var registerStep = RegisterStep.Create("stepId 1", typeof(BehaviorA), "description");
        var replacement = new ReplaceStep("stepId 2", typeof(BehaviorB));

        Assert.Throws<InvalidOperationException>(() => registerStep.Replace(replacement));
    }

    [Test]
    public void Replace_ShouldReplaceBehaviorType()
    {
        var registerStep = RegisterStep.Create("pipelineStep", typeof(BehaviorA), "description");
        var replacement = new ReplaceStep("pipelineStep", typeof(BehaviorB));

        registerStep.Replace(replacement);

        Assert.That(registerStep.BehaviorType, Is.EqualTo(typeof(BehaviorB)));
    }

    [Test]
    public void Replace_WhenReplacementContainsNoDescription_ShouldKeepOriginalDescription()
    {
        const string originalDescription = "description";
        var registerStep = RegisterStep.Create("pipelineStep", typeof(BehaviorA), originalDescription);
        var replacement = new ReplaceStep("pipelineStep", typeof(BehaviorB));

        registerStep.Replace(replacement);

        Assert.That(registerStep.Description, Is.EqualTo(originalDescription));
    }

    [Test]
    public void Replace_WhenReplacementContainsEmptyDescription_ShouldKeepOriginalDescription()
    {
        const string originalDescription = "description";
        var registerStep = RegisterStep.Create("pipelineStep", typeof(BehaviorA), originalDescription);
        var replacement = new ReplaceStep("pipelineStep", typeof(BehaviorB), "    ");

        registerStep.Replace(replacement);

        Assert.That(registerStep.Description, Is.EqualTo(originalDescription));
    }

    [Test]
    public void Replace_WhenReplacementContainsDescription_ShouldReplaceDescription()
    {
        const string replacementDescription = "new";
        var registerStep = RegisterStep.Create("pipelineStep", typeof(BehaviorA), "description");
        var replacement = new ReplaceStep("pipelineStep", typeof(BehaviorB), replacementDescription);

        registerStep.Replace(replacement);

        Assert.That(registerStep.Description, Is.EqualTo(replacementDescription));
    }

    [Test]
    public void Replace_WhenReplacementProvidesNoFactory_ShouldBuildReplacementFromBuilder()
    {
        var originalBehaviorFactoryCalled = false;
        Func<IServiceProvider, IBehavior> originalBehaviorFactory = b =>
        {
            originalBehaviorFactoryCalled = true;
            return new BehaviorA();
        };

        var builder = new FakeBuilder(typeof(BehaviorB));
        var registerStep = RegisterStep.Create("pipelineStep", typeof(BehaviorA), "description", originalBehaviorFactory);
        var replacement = new ReplaceStep("pipelineStep", typeof(BehaviorB));

        registerStep.Replace(replacement);
        var behavior = registerStep.CreateBehavior(builder);

        Assert.That(originalBehaviorFactoryCalled, Is.False);
        Assert.That(behavior, Is.InstanceOf<BehaviorB>());
    }

    [Test]
    public void Replace_WhenReplacementProvidedFactory_ShouldBuildReplacementFromFactory()
    {
        var replacementBehaviorFactoryCalled = false;
        Func<IServiceProvider, IBehavior> replacementBehaviorFactory = b =>
        {
            replacementBehaviorFactoryCalled = true;
            return new BehaviorB();
        };

        var builder = new FakeBuilder(typeof(BehaviorB));
        var registerStep = RegisterStep.Create("pipelineStep", typeof(BehaviorA), "description", b => { throw new Exception(); });
        var replacement = new ReplaceStep("pipelineStep", typeof(BehaviorB), factoryMethod: replacementBehaviorFactory);

        registerStep.Replace(replacement);
        var behavior = registerStep.CreateBehavior(builder);

        Assert.That(replacementBehaviorFactoryCalled, Is.True);
        Assert.That(behavior, Is.InstanceOf<BehaviorB>());
    }

    class BehaviorA : IBehavior<IRoutingContext, IRoutingContext>
    {
        public Task Invoke(IRoutingContext context, Func<IRoutingContext, Task> next)
        {
            return Task.CompletedTask;
        }
    }

    class BehaviorB : IBehavior<IRoutingContext, IRoutingContext>
    {
        public Task Invoke(IRoutingContext context, Func<IRoutingContext, Task> next)
        {
            return Task.CompletedTask;
        }
    }
}