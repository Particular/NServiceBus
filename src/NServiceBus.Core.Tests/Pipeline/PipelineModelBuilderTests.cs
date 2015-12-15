namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class PipelineModelBuilderTests
    {
        [Test]
        public void ShouldDetectConflictingStepRegistrations()
        {
            var builder = new PipelineModelBuilder(typeof(ParentContext), new List<RegisterStep>
            {
                RegisterStep.Create("Root1", typeof(RootBehavior), "desc"),
                RegisterStep.Create("Root1", typeof(NonReachableChildBehavior), "desc"),

            }, new List<RemoveStep>(), new List<ReplaceStep>());


            var ex = Assert.Throws<Exception>(() => builder.Build());

            Assert.AreEqual("Step registration with id 'Root1' is already registered for 'NServiceBus.Core.Tests.Pipeline.PipelineModelBuilderTests+RootBehavior'.", ex.Message);
        }

        [Test]
        public void ShouldOnlyAllowReplacementOfExistingRegistrations()
        {
            var builder = new PipelineModelBuilder(typeof(ParentContext), new List<RegisterStep>
            {
                RegisterStep.Create("Root1", typeof(RootBehavior), "desc"),

            }, new List<RemoveStep>(), new List<ReplaceStep>
            {
                new ReplaceStep("DoesNotExist", typeof(RootBehavior), "desc"),
            });


            var ex = Assert.Throws<Exception>(() => builder.Build());

            Assert.AreEqual("You can only replace an existing step registration, 'DoesNotExist' registration does not exist.", ex.Message);
        }

        [Test]
        public void ShouldOnlyAllowRemovalOfExistingRegistrations()
        {
            var builder = new PipelineModelBuilder(typeof(ParentContext), new List<RegisterStep>
            {
                RegisterStep.Create("Root1", typeof(RootBehavior), "desc"),

            }, new List<RemoveStep>
            {
                new RemoveStep("DoesNotExist"),
            }, new List<ReplaceStep>());


            var ex = Assert.Throws<Exception>(() => builder.Build());

            Assert.AreEqual("You cannot remove step registration with id 'DoesNotExist', registration does not exist.", ex.Message);
        }

        [Test]
        public void ShouldOnlyAllowRemovalWhenNoOtherDependsOnItsBeforeRegistration()
        {
            var someBehaviorRegistration = RegisterStep.Create("SomeBehavior", typeof(SomeBehavior), "desc");
            var anotherBehaviorRegistration = RegisterStep.Create("AnotherBehavior", typeof(AnotherBehavior), "desc");

            anotherBehaviorRegistration.InsertBefore("SomeBehavior");

            var builder = new PipelineModelBuilder(typeof(ParentContext), new List<RegisterStep>
            {
                someBehaviorRegistration,
                anotherBehaviorRegistration,

            }, new List<RemoveStep> { new RemoveStep("SomeBehavior")}, new List<ReplaceStep>());


            var ex = Assert.Throws<Exception>(() => builder.Build());

            Assert.AreEqual("You cannot remove step registration with id 'SomeBehavior', registration with id 'AnotherBehavior' depends on it.", ex.Message);
        }

        [Test]
        public void ShouldOnlyAllowRemovalWhenNoOtherDependsOnItsAfterRegistration()
        {
            var someBehaviorRegistration = RegisterStep.Create("SomeBehavior", typeof(SomeBehavior), "desc");
            var anotherBehaviorRegistration = RegisterStep.Create("AnotherBehavior", typeof(AnotherBehavior), "desc");

            anotherBehaviorRegistration.InsertAfter("SomeBehavior");

            var builder = new PipelineModelBuilder(typeof(ParentContext), new List<RegisterStep>
            {
                someBehaviorRegistration,
                anotherBehaviorRegistration,

            }, new List<RemoveStep> { new RemoveStep("SomeBehavior") }, new List<ReplaceStep>());


            var ex = Assert.Throws<Exception>(() => builder.Build());

            Assert.AreEqual("You cannot remove step registration with id 'SomeBehavior', registration with id 'AnotherBehavior' depends on it.", ex.Message);
        }

        [Test]
        public void ShouldDetectMissingBehaviorForRootContext()
        {
            var builder = new PipelineModelBuilder(typeof(ParentContext), new List<RegisterStep>
            {
                RegisterStep.Create("Child", typeof(ChildBehavior), "desc"),

            }, new List<RemoveStep>(), new List<ReplaceStep>());


            var ex = Assert.Throws<Exception>(() => builder.Build());

            Assert.AreEqual("Can't find any behaviors/connectors for the root context (NServiceBus.Core.Tests.Pipeline.PipelineModelBuilderTests+ParentContext)", ex.Message);
        }

        [Test]
        public void ShouldDetectConflictingStageConnectors()
        {
            var builder = new PipelineModelBuilder(typeof(ParentContext), new List<RegisterStep>
            {
                RegisterStep.Create("Root1", typeof(RootBehavior), "desc"),
                RegisterStep.Create("RootToChildConnector", typeof(RootToChildConnector), "desc"),
                RegisterStep.Create("RootToChild2Connector", typeof(RootToChild2Connector), "desc")

            }, new List<RemoveStep>(), new List<ReplaceStep>());


            var ex = Assert.Throws<Exception>(() => builder.Build());

            Assert.AreEqual("Multiple stage connectors found for stage 'NServiceBus.Core.Tests.Pipeline.PipelineModelBuilderTests+ParentContext'. Please remove one of: NServiceBus.Core.Tests.Pipeline.PipelineModelBuilderTests+RootToChildConnector;NServiceBus.Core.Tests.Pipeline.PipelineModelBuilderTests+RootToChild2Connector", ex.Message);
        }

        [Test]
        public void ShouldDetectRegistrationsWithContextsReachableFromTheRootContext()
        {
            var builder = new PipelineModelBuilder(typeof(ParentContext), new List<RegisterStep>
            {
                RegisterStep.Create("Root", typeof(RootBehavior), "desc"),
                RegisterStep.Create("RootToChildConnector", typeof(RootToChild2Connector), "desc"),
                RegisterStep.Create("Child", typeof(NonReachableChildBehavior), "desc")
            }, new List<RemoveStep>(), new List<ReplaceStep>());


            var model = builder.Build();

            Assert.AreEqual(3, model.Count);
        }

        [Test]
        public void ShouldHandleTheTerminator()
        {
            var builder = new PipelineModelBuilder(typeof(ParentContext), new List<RegisterStep>
            {
                RegisterStep.Create("Root1", typeof(RootBehavior), "desc"),
                RegisterStep.Create("RootToChildConnector", typeof(RootToChildConnector), "desc"),
                RegisterStep.Create("Terminator", typeof(Terminator), "desc")

            }, new List<RemoveStep>(), new List<ReplaceStep>());


            var model = builder.Build();

            Assert.AreEqual(3, model.Count);
        }

        class ParentContext : BehaviorContext
        {
            public ParentContext(IBehaviorContext parentContext)
                : base(parentContext)
            {
            }
        }

        class ChildContext : ParentContext
        {
            public ChildContext(IBehaviorContext parentContext)
                : base(parentContext)
            {
            }
        }

        class ChildContextReachableButNotInheritingFromRootContext : BehaviorContext
        {
            public ChildContextReachableButNotInheritingFromRootContext(IBehaviorContext parentContext)
                : base(parentContext)
            {
            }
        }

        class RootToChildConnector : StageConnector<ParentContext, ChildContext>
        {
            public override Task Invoke(ParentContext context, Func<ChildContext, Task> next)
            {
                throw new NotImplementedException();
            }
        }

        class Terminator : PipelineTerminator<ChildContext>
        {
            protected override Task Terminate(ChildContext context)
            {
                throw new NotImplementedException();
            }
        }

        class RootToChild2Connector : StageConnector<ParentContext, ChildContextReachableButNotInheritingFromRootContext>
        {
            public override Task Invoke(ParentContext context, Func<ChildContextReachableButNotInheritingFromRootContext, Task> next)
            {
                throw new NotImplementedException();
            }
        }
        class SomeBehavior : Behavior<ParentContext>
        {
            public override Task Invoke(ParentContext context, Func<Task> next)
            {
                throw new NotImplementedException();
            }
        }

        class AnotherBehavior : Behavior<ParentContext>
        {
            public override Task Invoke(ParentContext context, Func<Task> next)
            {
                throw new NotImplementedException();
            }
        }

        class RootBehavior : Behavior<ParentContext>
        {
            public override Task Invoke(ParentContext context, Func<Task> next)
            {
                throw new NotImplementedException();
            }
        }

        class ChildBehavior : Behavior<ChildContext>
        {
            public override Task Invoke(ChildContext context, Func<Task> next)
            {
                throw new NotImplementedException();
            }
        }

        class NonReachableChildBehavior : Behavior<ChildContextReachableButNotInheritingFromRootContext>
        {
            public override Task Invoke(ChildContextReachableButNotInheritingFromRootContext context, Func<Task> next)
            {
                throw new NotImplementedException();
            }
        }
    }

}