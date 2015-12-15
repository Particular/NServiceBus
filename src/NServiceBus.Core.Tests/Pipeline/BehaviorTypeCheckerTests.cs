namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Audit;
    using NServiceBus.Pipeline;
    using NServiceBus.Unicast.Transport;
    using NUnit.Framework;

    [TestFixture]
    public class BehaviorTypeCheckerTests
    {
        [Test]
        public void Should_not_throw_for_simple_behavior()
        {
            BehaviorTypeChecker.ThrowIfInvalid(typeof(ValidBehavior), "foo");
        }

        [Test]
        public void Should_not_throw_for_behavior_using_context_interfaces()
        {
            BehaviorTypeChecker.ThrowIfInvalid(typeof(BehaviorUsingContextInterface), "foo");
        }

        [Test]
        public void Should_not_throw_for_closed_generic_behavior()
        {
            BehaviorTypeChecker.ThrowIfInvalid(typeof(GenericBehavior<object>), "foo");
        }

        [Test]
        public void Should_throw_for_non_behavior()
        {
            Assert.Throws<ArgumentException>(() => BehaviorTypeChecker.ThrowIfInvalid(typeof(string), "foo"));
        }

        [Test]
        public void Should_throw_for_open_generic_behavior()
        {
            Assert.Throws<ArgumentException>(() => BehaviorTypeChecker.ThrowIfInvalid(typeof(GenericBehavior<>), "foo"));
        }

        [Test]
        public void Should_throw_for_abstract_behavior()
        {
            Assert.Throws<ArgumentException>(() => BehaviorTypeChecker.ThrowIfInvalid(typeof(AbstractBehavior), "foo"));
        }

        [Test]
        public void Should_throw_for_behavior_using_context_implementations_on_tfrom()
        {
            Assert.Throws<ArgumentException>(() => BehaviorTypeChecker.ThrowIfInvalid(typeof(BehaviorUsingContextImplementationOnTFrom), "foo"));
        }

        [Test]
        public void Should_throw_for_behavior_using_context_implementations_on_tto()
        {
            Assert.Throws<ArgumentException>(() => BehaviorTypeChecker.ThrowIfInvalid(typeof(BehaviorUsingContextImplementationOnTTo), "foo"));
        }

        class ValidBehavior : Behavior<IBehaviorContext>
        {
            public override Task Invoke(IBehaviorContext context, Func<Task> next)
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

            public void Initialize(PipelineInfo pipelineInfo)
            {
            }

            public Task Warmup()
            {
                return Task.FromResult(0);
            }

            public Task Cooldown()
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

        class GenericBehavior<T> : Behavior<IBehaviorContext>
        {
            public override Task Invoke(IBehaviorContext context, Func<Task> next)
            {
                return Task.FromResult(0);
            }
        }

        abstract class AbstractBehavior : Behavior<IBehaviorContext>
        {
        }
    }
}