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
        public void ShouldDetectConflictingStageConnectors()
        {
            var builder = new PipelineModelBuilder(typeof(ParentContext), new List<RegisterStep>
            {
                RegisterStep.Create("Root1", typeof(RootBehavior), "desc"),
                RegisterStep.Create("RootToChildConnector", typeof(RootToChildConnector), "desc"),
                RegisterStep.Create("RootToChild2Connector", typeof(RootToChild2Connector), "desc")
        
            }, new List<RemoveStep>(), new List<ReplaceBehavior>());


            var ex = Assert.Throws<Exception>(() => builder.Build());

            Assert.True(ex.Message.Contains("Multiple stage connectors found"));
        }

        [Test]
        public void ShouldDetectRegistrationsWithContextsReachableFromTheRootContext()
        {
            var builder = new PipelineModelBuilder(typeof(ParentContext), new List<RegisterStep>
            {
                RegisterStep.Create("Root", typeof(RootBehavior), "desc"),
                RegisterStep.Create("RootToChildConnector", typeof(RootToChild2Connector), "desc"),
                RegisterStep.Create("Child", typeof(NonReachableChildBehavior), "desc")
            }, new List<RemoveStep>(), new List<ReplaceBehavior>());


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
        
            }, new List<RemoveStep>(), new List<ReplaceBehavior>());


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