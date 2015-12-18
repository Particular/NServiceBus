namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Audit;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;

    [TestFixture]
    public class BehaviorTypeCheckerTests
    {
        const string Description = "foo";

        [Test]
        public void Should_not_throw_for_simple_behavior()
        {
            BehaviorTypeChecker.ThrowIfInvalid(typeof(ValidBehavior), Description);
        }

        [Test]
        public void Should_not_throw_for_behavior_using_context_interfaces()
        {
            BehaviorTypeChecker.ThrowIfInvalid(typeof(BehaviorUsingContextInterface), Description);
        }

        [Test]
        public void Should_not_throw_for_closed_generic_behavior()
        {
            BehaviorTypeChecker.ThrowIfInvalid(typeof(GenericBehavior<object>), Description);
        }

        [Test]
        public void Should_throw_for_non_behavior()
        {
            Assert.Throws<ArgumentException>(() => BehaviorTypeChecker.ThrowIfInvalid(typeof(string), Description));
        }

        [Test]
        public void Should_throw_for_open_generic_behavior()
        {
            Assert.Throws<ArgumentException>(() => BehaviorTypeChecker.ThrowIfInvalid(typeof(GenericBehavior<>), Description));
        }

        [Test]
        public void Should_throw_for_abstract_behavior()
        {
            Assert.Throws<ArgumentException>(() => BehaviorTypeChecker.ThrowIfInvalid(typeof(AbstractBehavior), Description));
        }

        [Test]
        public void Should_throw_for_behavior_using_context_implementations_on_tfrom()
        {
            Assert.Throws<ArgumentException>(() => BehaviorTypeChecker.ThrowIfInvalid(typeof(BehaviorUsingContextImplementationOnTFrom), Description));
        }

        [Test]
        public void Should_throw_for_behavior_using_context_implementations_on_tto()
        {
            Assert.Throws<ArgumentException>(() => BehaviorTypeChecker.ThrowIfInvalid(typeof(BehaviorUsingContextImplementationOnTTo), Description));
        }

        [Test]
        public void Should_throw_for_behavior_using_IIncomingContext()
        {
            Assert.Throws<ArgumentException>(() => BehaviorTypeChecker.ThrowIfInvalid(typeof(BehaviorUsingIncomingContext), Description));
        }

        [Test]
        public void Should_throw_for_behavior_using_IOutgoingContext()
        {
            Assert.Throws<ArgumentException>(() => BehaviorTypeChecker.ThrowIfInvalid(typeof(BehaviorUsingOutgoingContext), Description));
        }

        [Test]
        public void Should_throw_for_behavior_using_IBehaviorContext()
        {
            Assert.Throws<ArgumentException>(() => BehaviorTypeChecker.ThrowIfInvalid(typeof(BehaviorUsingBehaviorContext), Description));
        }

        interface IRootContext : IBehaviorContext { }

        class ValidBehavior : Behavior<IRootContext>
        {
            public override Task Invoke(IRootContext context, Func<Task> next)
            {
                return Task.FromResult(0);
            }
        }

        class BehaviorUsingBehaviorContext : Behavior<IBehaviorContext>
        {
            public override Task Invoke(IBehaviorContext context, Func<Task> next)
            {
                return Task.FromResult(0);
            }
        }

        class BehaviorUsingIncomingContext : Behavior<IIncomingContext>
        {
            public override Task Invoke(IIncomingContext context, Func<Task> next)
            {
                return Task.FromResult(0);
            }
        }

        class BehaviorUsingOutgoingContext : Behavior<IOutgoingContext>
        {
            public override Task Invoke(IOutgoingContext context, Func<Task> next)
            {
                return Task.FromResult(0);
            }
        }

        class BehaviorUsingContextImplementationOnTTo : IBehavior<IAuditContext, RootContext>
        {
            public Task Invoke(IAuditContext context, Func<RootContext, Task> next)
            {
                return Task.FromResult(0);
            }
        }

        class BehaviorUsingContextInterface : Behavior<IAuditContext>
        {
            public override Task Invoke(IAuditContext context, Func<Task> next)
            {
                return Task.FromResult(0);
            }
        }

        class BehaviorUsingContextImplementationOnTFrom : Behavior<RootContext>
        {
            public override Task Invoke(RootContext context, Func<Task> next)
            {
                return Task.FromResult(0);
            }
        }

        class GenericBehavior<T> : Behavior<IRootContext>
        {
            public override Task Invoke(IRootContext context, Func<Task> next)
            {
                return Task.FromResult(0);
            }
        }

        abstract class AbstractBehavior : Behavior<IRootContext>
        {
        }
    }
}