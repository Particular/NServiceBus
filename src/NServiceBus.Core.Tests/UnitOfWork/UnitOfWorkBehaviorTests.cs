namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Testing;
    using UnitOfWork;

    [TestFixture]
    public class UnitOfWorkBehaviorTests
    {
        [Test]
        public async Task Should_not_call_Begin_or_End_when_hasUnitsOfWork_is_false()
        {
            var builder = new FakeBuilder();

            var behavior = new UnitOfWorkBehavior();

            await InvokeBehavior(builder, behavior: behavior);

            var unitOfWork = new UnitOfWork();
            builder.Register<IManageUnitsOfWork>(unitOfWork);

            await InvokeBehavior(builder, behavior: behavior);

            Assert.IsFalse(unitOfWork.BeginCalled);
            Assert.IsFalse(unitOfWork.EndCalled);
        }

        [Test]
        public void When_first_throw_second_is_cleaned_up()
        {
            var builder = new FakeBuilder();

            var unitOfWorkThatThrowsFromEnd = new UnitOfWorkThatThrowsFromEnd();
            var unitOfWork = new UnitOfWork();

            builder.Register<IManageUnitsOfWork>(unitOfWorkThatThrowsFromEnd, unitOfWork);

            //since it is a single exception then it will not be an AggregateException
            Assert.That(async () => await InvokeBehavior(builder), Throws.InvalidOperationException);
            Assert.IsTrue(unitOfWorkThatThrowsFromEnd.BeginCalled);
            Assert.IsTrue(unitOfWorkThatThrowsFromEnd.EndCalled);
            Assert.IsTrue(unitOfWork.BeginCalled);
            Assert.IsTrue(unitOfWork.EndCalled);
        }

        [Test]
        public void Should_append_end_exception_to_rethrow()
        {
            var builder = new FakeBuilder();

            var unitOfWork = new UnitOfWorkThatThrowsFromEnd();

            builder.Register<IManageUnitsOfWork>(() => unitOfWork);

            //since it is a single exception then it will not be an AggregateException
            Assert.That(async () => await InvokeBehavior(builder), Throws.InvalidOperationException.And.SameAs(unitOfWork.ExceptionThrownFromEnd));
        }

        [Test]
        public void Should_not_invoke_end_if_begin_was_not_invoked()
        {
            var builder = new FakeBuilder();

            var unitOfWorkThatThrowsFromBegin = new UnitOfWorkThatThrowsFromBegin();
            var unitOfWork = new UnitOfWork();

            builder.Register<IManageUnitsOfWork>(unitOfWorkThatThrowsFromBegin, unitOfWork);

            //since it is a single exception then it will not be an AggregateException
            Assert.That(async () => await InvokeBehavior(builder), Throws.InvalidOperationException);
            Assert.False(unitOfWork.EndCalled);

        }

        [Test]
        public void Should_pass_exceptions_to_the_uow_end()
        {
            var builder = new FakeBuilder();

            var unitOfWork = new UnitOfWork();

            builder.Register<IManageUnitsOfWork>(() => unitOfWork);

            var ex = new Exception("Handler failed");
            //since it is a single exception then it will not be an AggregateException
            Assert.That(async () => await InvokeBehavior(builder, ex), Throws.InstanceOf<Exception>().And.SameAs(ex));
            Assert.AreSame(ex, unitOfWork.ExceptionPassedToEnd);
        }

        [Test]
        public async Task Should_invoke_ends_in_reverse_order_of_the_begins()
        {
            var builder = new FakeBuilder();

            var order = new List<string>();
            var firstUnitOfWork = new OrderAwareUnitOfWork("first", order);
            var secondUnitOfWork = new OrderAwareUnitOfWork("second", order);


            builder.Register<IManageUnitsOfWork>(firstUnitOfWork, secondUnitOfWork);

            await InvokeBehavior(builder);

            Assert.AreEqual("first", order[0]);
            Assert.AreEqual("second", order[1]);
            Assert.AreEqual("second", order[2]);
            Assert.AreEqual("first", order[3]);
        }

        [Test]
        public void Should_call_all_end_even_if_one_or_more_of_them_throws()
        {
            var builder = new FakeBuilder();

            var unitOfWorkThatThrows = new UnitOfWorkThatThrowsFromEnd();
            var unitOfWork = new UnitOfWork();

            builder.Register<IManageUnitsOfWork>(unitOfWorkThatThrows, unitOfWork);

            Assert.That(async () => await InvokeBehavior(builder), Throws.InvalidOperationException);
            Assert.True(unitOfWork.EndCalled);
        }

        [Test]
        public void Should_invoke_ends_on_all_begins_that_was_called_even_when_begin_throws()
        {
            var builder = new FakeBuilder();

            var normalUnitOfWork = new UnitOfWork();
            var unitOfWorkThatThrows = new UnitOfWorkThatThrowsFromBegin();
            var unitOfWorkThatIsNeverCalled = new UnitOfWork();

            builder.Register<IManageUnitsOfWork>(normalUnitOfWork, unitOfWorkThatThrows, unitOfWorkThatIsNeverCalled);

            Assert.That(async () => await InvokeBehavior(builder), Throws.InvalidOperationException);

            Assert.True(normalUnitOfWork.EndCalled);
            Assert.True(unitOfWorkThatThrows.EndCalled);
            Assert.False(unitOfWorkThatIsNeverCalled.EndCalled);
        }

        [Test]
        public void Should_throw_friendly_exception_if_IManageUnitsOfWork_Begin_returns_null()
        {
            var builder = new FakeBuilder();

            builder.Register<IManageUnitsOfWork>(() => new UnitOfWorkThatReturnsNullForBegin());
            Assert.That(async () => await InvokeBehavior(builder),
                Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
        }

        [Test]
        public void Should_throw_friendly_exception_if_IManageUnitsOfWork_End_returns_null()
        {
            var builder = new FakeBuilder();

            builder.Register<IManageUnitsOfWork>(() => new UnitOfWorkThatReturnsNullForEnd());
            Assert.That(async () => await InvokeBehavior(builder),
                Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
        }

        static Task InvokeBehavior(FakeBuilder builder, Exception toThrow = null, UnitOfWorkBehavior behavior = null)
        {
            var runner = behavior ?? new UnitOfWorkBehavior();

            var context = new TestableIncomingPhysicalMessageContext();
            context.Builder = builder;

            return runner.Invoke(context, ctx =>
            {
                if (toThrow != null)
                {
                    throw toThrow;
                }
                return TaskEx.CompletedTask;
            });
        }

        class UnitOfWorkThatThrowsFromEnd : IManageUnitsOfWork
        {
            public bool BeginCalled;
            public bool EndCalled;
            public Exception ExceptionThrownFromEnd = new InvalidOperationException();

            public Task Begin()
            {
                BeginCalled = true;
                return TaskEx.CompletedTask;
            }

            public Task End(Exception ex = null)
            {
                EndCalled = true;
                throw ExceptionThrownFromEnd;
            }

        }

        class UnitOfWorkThatThrowsFromBegin : IManageUnitsOfWork
        {
            public bool EndCalled;
            public Exception ExceptionThrownFromEnd = new InvalidOperationException();

            public Task Begin()
            {
                throw ExceptionThrownFromEnd;
            }

            public Task End(Exception ex = null)
            {
                EndCalled = true;
                return TaskEx.CompletedTask;
            }
        }

        class UnitOfWork : IManageUnitsOfWork
        {
            public bool BeginCalled;
            public bool EndCalled;
            public Exception ExceptionPassedToEnd;
            public Task Begin()
            {
                BeginCalled = true;
                return TaskEx.CompletedTask;
            }

            public Task End(Exception ex = null)
            {
                ExceptionPassedToEnd = ex;
                EndCalled = true;
                return TaskEx.CompletedTask;
            }
        }

        class UnitOfWorkThatReturnsNullForBegin : IManageUnitsOfWork
        {
            public Task Begin()
            {
                return null;
            }

            public Task End(Exception ex = null)
            {
                return TaskEx.CompletedTask;
            }
        }

        class UnitOfWorkThatReturnsNullForEnd : IManageUnitsOfWork
        {
            public Task Begin()
            {
                return TaskEx.CompletedTask;
            }

            public Task End(Exception ex = null)
            {
                return null;
            }
        }

        [Test]
        public async Task Verify_order()
        {
            var builder = new FakeBuilder();

            var unitOfWork1 = new CountingUnitOfWork();
            var unitOfWork2 = new CountingUnitOfWork();
            var unitOfWork3 = new CountingUnitOfWork();

            builder.Register<IManageUnitsOfWork>(unitOfWork1, unitOfWork2, unitOfWork3);

            await InvokeBehavior(builder);

            Assert.AreEqual(1, unitOfWork1.BeginCallIndex);
            Assert.AreEqual(2, unitOfWork2.BeginCallIndex);
            Assert.AreEqual(3, unitOfWork3.BeginCallIndex);
            Assert.AreEqual(3, unitOfWork1.EndCallIndex);
            Assert.AreEqual(2, unitOfWork2.EndCallIndex);
            Assert.AreEqual(1, unitOfWork3.EndCallIndex);
        }

        class CountingUnitOfWork : IManageUnitsOfWork
        {
            static int BeginCallCount;
            static int EndCallCount;
            public int EndCallIndex;
            public int BeginCallIndex;

            public Task Begin()
            {
                BeginCallCount++;
                BeginCallIndex = BeginCallCount;
                return TaskEx.CompletedTask;
            }
            public Task End(Exception ex = null)
            {
                EndCallCount++;
                EndCallIndex = EndCallCount;
                return TaskEx.CompletedTask;
            }
        }

        [Test]
        public void Should_pass_exception_to_cleanup()
        {
            var builder = new FakeBuilder();

            var unitOfWork = new CaptureExceptionPassedToEndUnitOfWork();
            var throwingUoW = new UnitOfWorkThatThrowsFromEnd();

            builder.Register<IManageUnitsOfWork>(unitOfWork, throwingUoW);

            //since it is a single exception then it will not be an AggregateException
            Assert.That(async () => await InvokeBehavior(builder), Throws.InstanceOf<InvalidOperationException>().And.SameAs(throwingUoW.ExceptionThrownFromEnd));
            Assert.AreSame(throwingUoW.ExceptionThrownFromEnd, unitOfWork.Exception);
        }

        class CaptureExceptionPassedToEndUnitOfWork : IManageUnitsOfWork
        {
            public Task Begin()
            {
                return TaskEx.CompletedTask;
            }
            public Task End(Exception ex = null)
            {
                Exception = ex;
                return TaskEx.CompletedTask;
            }
            public Exception Exception;
        }

        class OrderAwareUnitOfWork : IManageUnitsOfWork
        {
            string name;
            List<string> order;

            public OrderAwareUnitOfWork(string name, List<string> order)
            {
                this.name = name;
                this.order = order;
            }

            public Task Begin()
            {
                order.Add(name);
                return TaskEx.CompletedTask;
            }

            public Task End(Exception ex = null)
            {
                order.Add(name);
                return TaskEx.CompletedTask;
            }
        }
    }
}