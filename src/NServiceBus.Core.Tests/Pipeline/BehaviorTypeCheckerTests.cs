namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
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

        class ValidBehavior : IBehavior<IRootContext, IRootContext>
        {
            public Task Invoke(IRootContext context, Func<IRootContext, Task> next)
            {
                return Task.CompletedTask;
            }
        }

        class BehaviorUsingBehaviorContext : IBehavior<IBehaviorContext, IBehaviorContext>
        {
            public Task Invoke(IBehaviorContext context, Func<IBehaviorContext, Task> next)
            {
                return Task.CompletedTask;
            }
        }

        class BehaviorUsingIncomingContext : IBehavior<IIncomingContext, IIncomingContext>
        {
            public Task Invoke(IIncomingContext context, Func<IIncomingContext, Task> next)
            {
                return Task.CompletedTask;
            }
        }

        class BehaviorUsingOutgoingContext : IBehavior<IOutgoingContext, IOutgoingContext>
        {
            public Task Invoke(IOutgoingContext context, Func<IOutgoingContext, Task> next)
            {
                return Task.CompletedTask;
            }
        }

        class BehaviorUsingContextImplementationOnTTo : IBehavior<IAuditContext, RootContext>
        {
            public Task Invoke(IAuditContext context, Func<RootContext, Task> stage)
            {
                return Task.CompletedTask;
            }
        }

        class BehaviorUsingContextInterface : IBehavior<IAuditContext, IAuditContext>
        {
            public Task Invoke(IAuditContext context, Func<IAuditContext, Task> next)
            {
                return Task.CompletedTask;
            }
        }

        class BehaviorUsingContextImplementationOnTFrom : IBehavior<RootContext, RootContext>
        {
            public Task Invoke(RootContext context, Func<RootContext, Task> next)
            {
                return Task.CompletedTask;
            }
        }

        class GenericBehavior<T> : IBehavior<IRootContext, IRootContext>
        {
            public Task Invoke(IRootContext context, Func<IRootContext, Task> next)
            {
                return Task.CompletedTask;
            }
        }

        abstract class AbstractBehavior : IBehavior<IRootContext, IRootContext>
        {
            public abstract Task Invoke(IRootContext context, Func<IRootContext, Task> next);
        }
    }
}