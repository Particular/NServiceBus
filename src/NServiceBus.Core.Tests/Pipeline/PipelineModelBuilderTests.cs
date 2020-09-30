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
            var builder = new PipelineModelBuilder(typeof(IParentContext), new List<RegisterStep>
            {
                RegisterStep.Create("Root1", typeof(RootBehavior), "desc"),
                RegisterStep.Create("Root1", typeof(ChildBehaviorOfChildContextNotInheritedFromParentContext), "desc"),

            }, new List<RemoveStep>(), new List<ReplaceStep>(), new List<AddOrReplaceStep>());

            var ex = Assert.Throws<Exception>(() => builder.Build());

            Assert.AreEqual("Step registration with id 'Root1' is already registered for 'NServiceBus.Core.Tests.Pipeline.PipelineModelBuilderTests+RootBehavior'.", ex.Message);
        }

        [Test]
        public void ShouldOnlyAllowReplacementOfExistingRegistrations()
        {
            var builder = new PipelineModelBuilder(typeof(IParentContext), new List<RegisterStep>
            {
                RegisterStep.Create("Root1", typeof(RootBehavior), "desc"),

            }, new List<RemoveStep>(), new List<ReplaceStep>
            {
                new ReplaceStep("DoesNotExist", typeof(RootBehavior), "desc"),
            },
            new List<AddOrReplaceStep>());

            var ex = Assert.Throws<Exception>(() => builder.Build());

            Assert.AreEqual("You can only replace an existing step registration, 'DoesNotExist' registration does not exist.", ex.Message);
        }

        [Test]
        public void ShouldAddWhenAddingOrReplacingABehaviorThatDoesntExist()
        {
            var builder = new PipelineModelBuilder(typeof(IParentContext),
                new List<RegisterStep>()
                {
                    RegisterStep.Create("Root1", typeof(RootBehavior), "desc")
                },
                new List<RemoveStep>(),
                new List<ReplaceStep>(),
                new List<AddOrReplaceStep>()
                {
                    AddOrReplaceStep.Create("SomeBehaviorOfParentContext", typeof(SomeBehaviorOfParentContext), "desc")
                });

            var model = builder.Build();

            Assert.That(model.Count, Is.EqualTo(2));
            var addedBehavior = model.FirstOrDefault(x => x.StepId == "SomeBehaviorOfParentContext");
            Assert.That(addedBehavior, Is.Not.Null);
            Assert.That(addedBehavior.BehaviorType, Is.EqualTo(typeof(SomeBehaviorOfParentContext)));
        }

        [Test]
        public void ShouldReplaceWhenAddingOrReplacingABehaviorThatDoesAlreadyExist()
        {
            var builder = new PipelineModelBuilder(typeof(IParentContext),
                new List<RegisterStep>()
                {
                    RegisterStep.Create("Root1", typeof(RootBehavior), "desc"),
                    RegisterStep.Create("SomeBehaviorOfParentContext", typeof(SomeBehaviorOfParentContext), "desc")
                },
                new List<RemoveStep>(),
                new List<ReplaceStep>(),
                new List<AddOrReplaceStep>()
                {
                    AddOrReplaceStep.Create("SomeBehaviorOfParentContext", typeof(AnotherBehaviorOfParentContext), "desc")
                });


            var model = builder.Build();

            Assert.That(model.Count, Is.EqualTo(2));
            var overriddenBehavior = model.FirstOrDefault(x => x.StepId == "SomeBehaviorOfParentContext");
            Assert.That(overriddenBehavior, Is.Not.Null);
            Assert.That(overriddenBehavior.BehaviorType, Is.EqualTo(typeof(AnotherBehaviorOfParentContext)));
        }

        [Test]
        public void ShouldOnlyAllowRemovalOfExistingRegistrations()
        {
            var builder = new PipelineModelBuilder(typeof(IParentContext), new List<RegisterStep>
            {
                RegisterStep.Create("Root1", typeof(RootBehavior), "desc"),

            }, new List<RemoveStep>
            {
                new RemoveStep("DoesNotExist"),
            },
            new List<ReplaceStep>(), new List<AddOrReplaceStep>());

            var ex = Assert.Throws<Exception>(() => builder.Build());

            Assert.AreEqual("You cannot remove step registration with id 'DoesNotExist', registration does not exist.", ex.Message);
        }

        [Test]
        public void ShouldOnlyAllowRemovalWhenNoOtherDependsOnItsBeforeRegistration()
        {
            var someBehaviorRegistration = RegisterStep.Create("SomeBehaviorOfParentContext", typeof(SomeBehaviorOfParentContext), "desc");
            var anotherBehaviorRegistration = RegisterStep.Create("AnotherBehaviorOfParentContext", typeof(AnotherBehaviorOfParentContext), "desc");

            anotherBehaviorRegistration.InsertBefore("SomeBehaviorOfParentContext");

            var builder = new PipelineModelBuilder(typeof(IParentContext), new List<RegisterStep>
            {
                someBehaviorRegistration,
                anotherBehaviorRegistration,

            },
            new List<RemoveStep>
            {
                new RemoveStep("SomeBehaviorOfParentContext")
            },
            new List<ReplaceStep>(), new List<AddOrReplaceStep>());

            var ex = Assert.Throws<Exception>(() => builder.Build());

            Assert.AreEqual("You cannot remove step registration with id 'SomeBehaviorOfParentContext', registration with id 'AnotherBehaviorOfParentContext' depends on it.", ex.Message);
        }

        [Test]
        public void ShouldOnlyAllowRemovalWhenNoOtherDependsOnItsAfterRegistration()
        {
            var someBehaviorRegistration = RegisterStep.Create("SomeBehaviorOfParentContext", typeof(SomeBehaviorOfParentContext), "desc");
            var anotherBehaviorRegistration = RegisterStep.Create("AnotherBehaviorOfParentContext", typeof(AnotherBehaviorOfParentContext), "desc");

            anotherBehaviorRegistration.InsertAfter("SomeBehaviorOfParentContext");

            var builder = new PipelineModelBuilder(typeof(IParentContext), new List<RegisterStep>
            {
                someBehaviorRegistration,
                anotherBehaviorRegistration,

            }, new List<RemoveStep> { new RemoveStep("SomeBehaviorOfParentContext") },
            new List<ReplaceStep>(), new List<AddOrReplaceStep>());


            var ex = Assert.Throws<Exception>(() => builder.Build());

            Assert.AreEqual("You cannot remove step registration with id 'SomeBehaviorOfParentContext', registration with id 'AnotherBehaviorOfParentContext' depends on it.", ex.Message);
        }

        [Test]
        public void ShouldDetectMissingBehaviorForRootContext()
        {
            var builder = new PipelineModelBuilder(typeof(IParentContext), new List<RegisterStep>
            {
                RegisterStep.Create("Child", typeof(ChildBehaviorOfChildContext), "desc"),

            }, new List<RemoveStep>(), new List<ReplaceStep>(), new List<AddOrReplaceStep>());

            var ex = Assert.Throws<Exception>(() => builder.Build());

            Assert.AreEqual("Can't find any behaviors/connectors for the root context (NServiceBus.Core.Tests.Pipeline.PipelineModelBuilderTests+IParentContext)", ex.Message);
        }

        [Test]
        public void ShouldDetectConflictingStageConnectors()
        {
            var builder = new PipelineModelBuilder(typeof(IParentContext), new List<RegisterStep>
            {
                RegisterStep.Create("Root1", typeof(RootBehavior), "desc"),
                RegisterStep.Create("ParentContextToChildContextConnector", typeof(ParentContextToChildContextConnector), "desc"),
                RegisterStep.Create("ParentContextToChildContextNotInheritedFromParentContextConnector", typeof(ParentContextToChildContextNotInheritedFromParentContextConnector), "desc")

            }, new List<RemoveStep>(), new List<ReplaceStep>(), new List<AddOrReplaceStep>());

            var ex = Assert.Throws<Exception>(() => builder.Build());

            Assert.AreEqual("Multiple stage connectors found for stage 'NServiceBus.Core.Tests.Pipeline.PipelineModelBuilderTests+IParentContext'. Remove one of: 'NServiceBus.Core.Tests.Pipeline.PipelineModelBuilderTests+ParentContextToChildContextConnector', 'NServiceBus.Core.Tests.Pipeline.PipelineModelBuilderTests+ParentContextToChildContextNotInheritedFromParentContextConnector'", ex.Message);
        }

        [Test]
        public void ShouldDetectNonExistingInsertAfterRegistrations()
        {
            var someBehaviorRegistration = RegisterStep.Create("SomeBehaviorOfParentContext", typeof(SomeBehaviorOfParentContext), "desc");
            var anotherBehaviorRegistration = RegisterStep.Create("AnotherBehaviorOfParentContext", typeof(AnotherBehaviorOfParentContext), "desc");

            anotherBehaviorRegistration.InsertAfter("DoesNotExist");

            var builder = new PipelineModelBuilder(typeof(IParentContext), new List<RegisterStep>
            {
                someBehaviorRegistration,
                anotherBehaviorRegistration,

            }, new List<RemoveStep>(), new List<ReplaceStep>(), new List<AddOrReplaceStep>());

            var ex = Assert.Throws<Exception>(() => builder.Build());

            Assert.AreEqual("Registration 'DoesNotExist' specified in the insertafter of the 'AnotherBehaviorOfParentContext' step does not exist. Current StepIds: 'SomeBehaviorOfParentContext', 'AnotherBehaviorOfParentContext'", ex.Message);
        }

        [Test]
        public void ShouldDetectNonExistingInsertBeforeRegistrations()
        {
            var someBehaviorRegistration = RegisterStep.Create("SomeBehaviorOfParentContext", typeof(SomeBehaviorOfParentContext), "desc");
            var anotherBehaviorRegistration = RegisterStep.Create("AnotherBehaviorOfParentContext", typeof(AnotherBehaviorOfParentContext), "desc");

            anotherBehaviorRegistration.InsertBefore("DoesNotExist");

            var builder = new PipelineModelBuilder(typeof(IParentContext), new List<RegisterStep>
            {
                someBehaviorRegistration,
                anotherBehaviorRegistration,

            }, new List<RemoveStep>(), new List<ReplaceStep>(), new List<AddOrReplaceStep>());


            var ex = Assert.Throws<Exception>(() => builder.Build());

            Assert.AreEqual("Registration 'DoesNotExist' specified in the insertbefore of the 'AnotherBehaviorOfParentContext' step does not exist. Current StepIds: 'SomeBehaviorOfParentContext', 'AnotherBehaviorOfParentContext'", ex.Message);
        }

        [Test]
        public void ShouldDetectRegistrationsWithContextsReachableFromTheRootContext()
        {
            var builder = new PipelineModelBuilder(typeof(IParentContext), new List<RegisterStep>
            {
                RegisterStep.Create("Root", typeof(RootBehavior), "desc"),
                RegisterStep.Create("ParentContextToChildContextNotInheritedFromParentContextConnector", typeof(ParentContextToChildContextNotInheritedFromParentContextConnector), "desc"),
                RegisterStep.Create("Child", typeof(ChildBehaviorOfChildContextNotInheritedFromParentContext), "desc")
            }, new List<RemoveStep>(), new List<ReplaceStep>(), new List<AddOrReplaceStep>());


            var model = builder.Build();

            Assert.AreEqual(3, model.Count);
        }

        [Test]
        public void ShouldDetectRegistrationsWithContextsNotReachableFromTheRootContext()
        {
            var builder = new PipelineModelBuilder(typeof(IParentContext), new List<RegisterStep>
            {
                RegisterStep.Create("Root", typeof(RootBehavior), "desc"),
                RegisterStep.Create("ParentContextToChildContextConnector", typeof(ParentContextToChildContextConnector), "desc"),
                RegisterStep.Create("Child", typeof(ChildBehaviorOfChildContextNotInheritedFromParentContext), "desc")
            }, new List<RemoveStep>(), new List<ReplaceStep>(), new List<AddOrReplaceStep>());


            var model = builder.Build();

            Assert.AreEqual(2, model.Count);
        }

        [Test]
        public void ShouldHandleTheTerminator()
        {
            var builder = new PipelineModelBuilder(typeof(IParentContext), new List<RegisterStep>
            {
                RegisterStep.Create("Root1", typeof(RootBehavior), "desc"),
                RegisterStep.Create("ParentContextToChildContextConnector", typeof(ParentContextToChildContextConnector), "desc"),
                RegisterStep.Create("Child", typeof(ChildBehaviorOfChildContextNotInheritedFromParentContext), "desc"),
                RegisterStep.Create("Terminator", typeof(Terminator), "desc")

            }, new List<RemoveStep>(), new List<ReplaceStep>(), new List<AddOrReplaceStep>());


            var model = builder.Build();

            Assert.AreEqual(3, model.Count);
        }

        interface IParentContext : IBehaviorContext { }

        class ParentContext : BehaviorContext, IParentContext
        {
            public ParentContext(IBehaviorContext parentContext)
                : base(parentContext)
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
                : base(parentContext)
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