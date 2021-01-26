namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class PipelineModelBuilderTests
    {
        [Test]
        public void ShouldDetectConflictingStepRegistrations()
        {
            var builder = ConfigurePipelineModelBuilder.Setup()
                .Register(RegisterStep.Create("Root1", typeof(RootBehavior), "desc"))
                .Register(RegisterStep.Create("Root1", typeof(ChildBehaviorOfChildContextNotInheritedFromParentContext), "desc"))
                .Build(typeof(IParentContext));

            var ex = Assert.Throws<Exception>(() => builder.Build());

            Assert.AreEqual("Step registration with id 'Root1' is already registered for 'NServiceBus.Core.Tests.Pipeline.PipelineModelBuilderTests+RootBehavior'.", ex.Message);
        }

        [Test]
        public void ShouldOnlyAllowReplacementOfExistingRegistrations()
        {
            var builder = ConfigurePipelineModelBuilder.Setup()
                .Register(RegisterStep.Create("Root1", typeof(RootBehavior), "desc"))
                .Replace(new ReplaceStep("DoesNotExist", typeof(RootBehavior), "desc"))
                .Build(typeof(IParentContext));

            var ex = Assert.Throws<Exception>(() => builder.Build());

            Assert.AreEqual("Multiple replacements of the same pipeline behaviour is not supported. Make sure that you only register a single replacement for 'DoesNotExist'.", ex.Message);
        }

        [Test]
        public void ShouldAddWhenAddingOrReplacingABehaviorThatDoesntExist()
        {
            var builder = ConfigurePipelineModelBuilder.Setup()
                .Register(RegisterStep.Create("Root1", typeof(RootBehavior), "desc"))
                .RegisterOrReplace(RegisterOrReplaceStep.Create("SomeBehaviorOfParentContext", typeof(SomeBehaviorOfParentContext), "desc"))
                .Build(typeof(IParentContext));

            var model = builder.Build();

            Assert.That(model.Count, Is.EqualTo(2));
            var addedBehavior = model.FirstOrDefault(x => x.StepId == "SomeBehaviorOfParentContext");
            Assert.That(addedBehavior, Is.Not.Null);
            Assert.That(addedBehavior.BehaviorType, Is.EqualTo(typeof(SomeBehaviorOfParentContext)));
        }

        [Test]
        public void ShouldReplaceWhenAddingOrReplacingABehaviorThatDoesAlreadyExist()
        {
            var builder = ConfigurePipelineModelBuilder.Setup()
                .Register(RegisterStep.Create("Root1", typeof(RootBehavior), "desc"))
                .Register(RegisterStep.Create("SomeBehaviorOfParentContext", typeof(SomeBehaviorOfParentContext), "desc"))
                .RegisterOrReplace(RegisterOrReplaceStep.Create("SomeBehaviorOfParentContext", typeof(AnotherBehaviorOfParentContext), "desc"))
                .Build(typeof(IParentContext));

            var model = builder.Build();

            Assert.That(model.Count, Is.EqualTo(2));
            var overriddenBehavior = model.FirstOrDefault(x => x.StepId == "SomeBehaviorOfParentContext");
            Assert.That(overriddenBehavior, Is.Not.Null);
            Assert.That(overriddenBehavior.BehaviorType, Is.EqualTo(typeof(AnotherBehaviorOfParentContext)));
        }

        [Test]
        public void ShouldDetectMissingBehaviorForRootContext()
        {
            var builder = ConfigurePipelineModelBuilder.Setup()
                .Register(RegisterStep.Create("Child", typeof(ChildBehaviorOfChildContext), "desc"))
                .Build(typeof(IParentContext));

            var ex = Assert.Throws<Exception>(() => builder.Build());

            Assert.AreEqual("Can't find any behaviors/connectors for the root context (NServiceBus.Core.Tests.Pipeline.PipelineModelBuilderTests+IParentContext)", ex.Message);
        }

        [Test]
        public void ShouldDetectConflictingStageConnectors()
        {
            var builder = ConfigurePipelineModelBuilder.Setup()
                .Register(RegisterStep.Create("Root1", typeof(RootBehavior), "desc"))
                .Register(RegisterStep.Create("ParentContextToChildContextConnector", typeof(ParentContextToChildContextConnector), "desc"))
                .Register(RegisterStep.Create("ParentContextToChildContextNotInheritedFromParentContextConnector", typeof(ParentContextToChildContextNotInheritedFromParentContextConnector), "desc"))
                .Build(typeof(IParentContext));

            var ex = Assert.Throws<Exception>(() => builder.Build());

            Assert.AreEqual("Multiple stage connectors found for stage 'NServiceBus.Core.Tests.Pipeline.PipelineModelBuilderTests+IParentContext'. Remove one of: 'NServiceBus.Core.Tests.Pipeline.PipelineModelBuilderTests+ParentContextToChildContextConnector', 'NServiceBus.Core.Tests.Pipeline.PipelineModelBuilderTests+ParentContextToChildContextNotInheritedFromParentContextConnector'", ex.Message);
        }

        [Test]
        public void ShouldDetectNonExistingInsertAfterRegistrations()
        {
            var someBehaviorRegistration = RegisterStep.Create("SomeBehaviorOfParentContext", typeof(SomeBehaviorOfParentContext), "desc");
            var anotherBehaviorRegistration = RegisterStep.Create("AnotherBehaviorOfParentContext", typeof(AnotherBehaviorOfParentContext), "desc");

            anotherBehaviorRegistration.InsertAfter("DoesNotExist");

            var builder = ConfigurePipelineModelBuilder.Setup()
                .Register(someBehaviorRegistration)
                .Register(anotherBehaviorRegistration)
                .Build(typeof(IParentContext));

            var ex = Assert.Throws<Exception>(() => builder.Build());

            Assert.AreEqual("Registration 'DoesNotExist' specified in the insertafter of the 'AnotherBehaviorOfParentContext' step does not exist. Current StepIds: 'SomeBehaviorOfParentContext', 'AnotherBehaviorOfParentContext'", ex.Message);
        }

        [Test]
        public void ShouldDetectNonExistingInsertBeforeRegistrations()
        {
            var someBehaviorRegistration = RegisterStep.Create("SomeBehaviorOfParentContext", typeof(SomeBehaviorOfParentContext), "desc");
            var anotherBehaviorRegistration = RegisterStep.Create("AnotherBehaviorOfParentContext", typeof(AnotherBehaviorOfParentContext), "desc");

            anotherBehaviorRegistration.InsertBefore("DoesNotExist");

            var builder = ConfigurePipelineModelBuilder.Setup()
                .Register(someBehaviorRegistration)
                .Register(anotherBehaviorRegistration)
                .Build(typeof(IParentContext));

            var ex = Assert.Throws<Exception>(() => builder.Build());

            Assert.AreEqual("Registration 'DoesNotExist' specified in the insertbefore of the 'AnotherBehaviorOfParentContext' step does not exist. Current StepIds: 'SomeBehaviorOfParentContext', 'AnotherBehaviorOfParentContext'", ex.Message);
        }

        [Test]
        public void ShouldDetectRegistrationsWithContextsReachableFromTheRootContext()
        {
            var builder = ConfigurePipelineModelBuilder.Setup()
                .Register(RegisterStep.Create("Root", typeof(RootBehavior), "desc"))
                .Register(RegisterStep.Create("ParentContextToChildContextNotInheritedFromParentContextConnector", typeof(ParentContextToChildContextNotInheritedFromParentContextConnector), "desc"))
                .Register(RegisterStep.Create("Child", typeof(ChildBehaviorOfChildContextNotInheritedFromParentContext), "desc"))
                .Build(typeof(IParentContext));

            var model = builder.Build();

            Assert.AreEqual(3, model.Count);
        }

        [Test]
        public void ShouldDetectRegistrationsWithContextsNotReachableFromTheRootContext()
        {
            var builder = ConfigurePipelineModelBuilder.Setup()
                .Register(RegisterStep.Create("Root", typeof(RootBehavior), "desc"))
                .Register(RegisterStep.Create("ParentContextToChildContextConnector", typeof(ParentContextToChildContextConnector), "desc"))
                .Register(RegisterStep.Create("Child", typeof(ChildBehaviorOfChildContextNotInheritedFromParentContext), "desc"))
                .Build(typeof(IParentContext));

            var model = builder.Build();

            Assert.AreEqual(2, model.Count);
        }

        [Test]
        public void ShouldHandleTheTerminator()
        {
            var builder = ConfigurePipelineModelBuilder.Setup()
                .Register(RegisterStep.Create("Root1", typeof(RootBehavior), "desc"))
                .Register(RegisterStep.Create("ParentContextToChildContextConnector", typeof(ParentContextToChildContextConnector), "desc"))
                .Register(RegisterStep.Create("Child", typeof(ChildBehaviorOfChildContextNotInheritedFromParentContext), "desc"))
                .Register(RegisterStep.Create("Terminator", typeof(Terminator), "desc"))
                .Build(typeof(IParentContext));

            var model = builder.Build();

            Assert.AreEqual(3, model.Count);
        }

        class ConfigurePipelineModelBuilder
        {
            List<RegisterStep> registrations = new List<RegisterStep>();
            List<RegisterOrReplaceStep> registerOrReplacements = new List<RegisterOrReplaceStep>();
            List<ReplaceStep> replacements = new List<ReplaceStep>();

            public static ConfigurePipelineModelBuilder Setup()
            {
                return new ConfigurePipelineModelBuilder();
            }

            public ConfigurePipelineModelBuilder Register(RegisterStep registration)
            {
                registrations.Add(registration);
                return this;
            }

            public ConfigurePipelineModelBuilder Replace(ReplaceStep registration)
            {
                replacements.Add(registration);
                return this;
            }

            public ConfigurePipelineModelBuilder RegisterOrReplace(RegisterOrReplaceStep registration)
            {
                registerOrReplacements.Add(registration);
                return this;
            }

            public PipelineModelBuilder Build(Type parentContextType)
            {
                return new PipelineModelBuilder(parentContextType, registrations, replacements, registerOrReplacements);
            }
        }

        interface IParentContext : IBehaviorContext { }

        class ParentContext : BehaviorContext, IParentContext
        {
            public ParentContext(IBehaviorContext parentContext)
                : base(parentContext, default)
            {
            }
        }

        interface IChildContext : IParentContext { }

        class ChildContext : ParentContext, IChildContext
        {
            public ChildContext(IBehaviorContext parentContext)
                : base(parentContext)
            {
            }
        }

        interface IChildContextNotInheritedFromParentContext : IBehaviorContext { }

        class ChildContextNotInheritedFromParentContext : BehaviorContext
        {
            public ChildContextNotInheritedFromParentContext(IBehaviorContext parentContext)
                : base(parentContext, default)
            {
            }
        }

        class ParentContextToChildContextConnector : StageConnector<IParentContext, IChildContext>
        {
            public override Task Invoke(IParentContext context, Func<IChildContext, Task> stage)
            {
                throw new NotImplementedException();
            }
        }

        class Terminator : PipelineTerminator<IChildContext>
        {
            protected override Task Terminate(IChildContext context)
            {
                throw new NotImplementedException();
            }
        }

        class ParentContextToChildContextNotInheritedFromParentContextConnector : StageConnector<IParentContext, IChildContextNotInheritedFromParentContext>
        {
            public override Task Invoke(IParentContext context, Func<IChildContextNotInheritedFromParentContext, Task> stage)
            {
                throw new NotImplementedException();
            }
        }

        class SomeBehaviorOfParentContext : IBehavior<IParentContext, IParentContext>
        {
            public Task Invoke(IParentContext context, Func<IParentContext, Task> next)
            {
                throw new NotImplementedException();
            }
        }

        class AnotherBehaviorOfParentContext : IBehavior<IParentContext, IParentContext>
        {
            public Task Invoke(IParentContext context, Func<IParentContext, Task> next)
            {
                throw new NotImplementedException();
            }
        }

        class RootBehavior : IBehavior<IParentContext, IParentContext>
        {
            public Task Invoke(IParentContext context, Func<IParentContext, Task> next)
            {
                throw new NotImplementedException();
            }
        }

        class ChildBehaviorOfChildContext : IBehavior<IChildContext, IChildContext>
        {
            public Task Invoke(IChildContext context, Func<IChildContext, Task> next)
            {
                throw new NotImplementedException();
            }
        }

        class ChildBehaviorOfChildContextNotInheritedFromParentContext : IBehavior<IChildContextNotInheritedFromParentContext, IChildContextNotInheritedFromParentContext>
        {
            public Task Invoke(IChildContextNotInheritedFromParentContext context, Func<IChildContextNotInheritedFromParentContext, Task> next)
            {
                throw new NotImplementedException();
            }
        }
    }

}