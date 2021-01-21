#pragma warning disable CS0618
namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Testing;
    using UnitOfWork;

    [TestFixture]
    public class UnitOfWorkBehaviorTests
    {
        [Test]
        public async Task Should_not_call_Begin_or_End_when_hasUnitsOfWork_is_false()
        {
            var services = new ServiceCollection();

            var behavior = new UnitOfWorkBehavior();

            await InvokeBehavior(services, behavior: behavior);

            var unitOfWork = new UnitOfWork();
            services.AddTransient<IManageUnitsOfWork>(sp => unitOfWork);

            await InvokeBehavior(services, behavior: behavior);

            Assert.IsFalse(unitOfWork.BeginCalled);
            Assert.IsFalse(unitOfWork.EndCalled);
        }

        [Test]
        public void When_first_throw_second_is_cleaned_up()
        {
            var services = new ServiceCollection();

            var unitOfWorkThatThrowsFromEnd = new UnitOfWorkThatThrowsFromEnd();
            var unitOfWork = new UnitOfWork();

            services.AddTransient<IManageUnitsOfWork>(sp => unitOfWorkThatThrowsFromEnd);
            services.AddTransient<IManageUnitsOfWork>(sp => unitOfWork);

            //since it is a single exception then it will not be an AggregateException
            Assert.That(async () => await InvokeBehavior(services), Throws.InvalidOperationException);
            Assert.IsTrue(unitOfWorkThatThrowsFromEnd.BeginCalled);
            Assert.IsTrue(unitOfWorkThatThrowsFromEnd.EndCalled);
            Assert.IsTrue(unitOfWork.BeginCalled);
            Assert.IsTrue(unitOfWork.EndCalled);
        }

        [Test]
        public void Should_append_end_exception_to_rethrow()
        {
            var unitOfWork = new UnitOfWorkThatThrowsFromEnd();

            var services = new ServiceCollection();
            services.AddTransient<IManageUnitsOfWork>(sp => unitOfWork);

            //since it is a single exception then it will not be an AggregateException
            Assert.That(async () => await InvokeBehavior(services), Throws.InvalidOperationException.And.SameAs(unitOfWork.ExceptionThrownFromEnd));
        }

        [Test]
        public void Should_not_invoke_end_if_begin_was_not_invoked()
        {
            var services = new ServiceCollection();

            var unitOfWorkThatThrowsFromBegin = new UnitOfWorkThatThrowsFromBegin();
            var unitOfWork = new UnitOfWork();

            services.AddTransient<IManageUnitsOfWork>(sp => unitOfWorkThatThrowsFromBegin);
            services.AddTransient<IManageUnitsOfWork>(sp => unitOfWork);

            //since it is a single exception then it will not be an AggregateException
            Assert.That(async () => await InvokeBehavior(services), Throws.InvalidOperationException);
            Assert.False(unitOfWork.EndCalled);
        }

        [Test]
        public void Should_pass_exceptions_to_the_uow_end()
        {
            var services = new ServiceCollection();

            var unitOfWork = new UnitOfWork();

            services.AddTransient<IManageUnitsOfWork>(sp => unitOfWork);

            var ex = new Exception("Handler failed");
            //since it is a single exception then it will not be an AggregateException
            Assert.That(async () => await InvokeBehavior(services, ex), Throws.InstanceOf<Exception>().And.SameAs(ex));
            Assert.AreSame(ex, unitOfWork.ExceptionPassedToEnd);
        }

        [Test]
        public async Task Should_invoke_ends_in_reverse_order_of_the_begins()
        {
            var services = new ServiceCollection();

            var order = new List<string>();
            var firstUnitOfWork = new OrderAwareUnitOfWork("first", order);
            var secondUnitOfWork = new OrderAwareUnitOfWork("second", order);

            services.AddTransient<IManageUnitsOfWork>(sp => firstUnitOfWork);
            services.AddTransient<IManageUnitsOfWork>(sp => secondUnitOfWork);

            await InvokeBehavior(services);

            Assert.AreEqual("first", order[0]);
            Assert.AreEqual("second", order[1]);
            Assert.AreEqual("second", order[2]);
            Assert.AreEqual("first", order[3]);
        }

        [Test]
        public void Should_call_all_end_even_if_one_or_more_of_them_throws()
        {
            var services = new ServiceCollection();

            var unitOfWorkThatThrows = new UnitOfWorkThatThrowsFromEnd();
            var unitOfWork = new UnitOfWork();

            services.AddTransient<IManageUnitsOfWork>(sp => unitOfWorkThatThrows);
            services.AddTransient<IManageUnitsOfWork>(sp => unitOfWork);

            Assert.That(async () => await InvokeBehavior(services), Throws.InvalidOperationException);
            Assert.True(unitOfWork.EndCalled);
        }

        [Test]
        public void Should_invoke_ends_on_all_begins_that_was_called_even_when_begin_throws()
        {
            var services = new ServiceCollection();

            var normalUnitOfWork = new UnitOfWork();
            var unitOfWorkThatThrows = new UnitOfWorkThatThrowsFromBegin();
            var unitOfWorkThatIsNeverCalled = new UnitOfWork();

            services.AddTransient<IManageUnitsOfWork>(sp => normalUnitOfWork);
            services.AddTransient<IManageUnitsOfWork>(sp => unitOfWorkThatThrows);
            services.AddTransient<IManageUnitsOfWork>(sp => unitOfWorkThatIsNeverCalled);

            Assert.That(async () => await InvokeBehavior(services), Throws.InvalidOperationException);

            Assert.True(normalUnitOfWork.EndCalled);
            Assert.True(unitOfWorkThatThrows.EndCalled);
            Assert.False(unitOfWorkThatIsNeverCalled.EndCalled);
        }

        [Test]
        public void Should_throw_friendly_exception_if_IManageUnitsOfWork_Begin_returns_null()
        {
            var services = new ServiceCollection();

            services.AddTransient<IManageUnitsOfWork>(sp => new UnitOfWorkThatReturnsNullForBegin());
            Assert.That(async () => await InvokeBehavior(services),
                Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
        }

        [Test]
        public void Should_throw_friendly_exception_if_IManageUnitsOfWork_End_returns_null()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IManageUnitsOfWork>(sp => new UnitOfWorkThatReturnsNullForEnd());

            Assert.That(async () => await InvokeBehavior(services),
                Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
        }

        static Task InvokeBehavior(IServiceCollection services, Exception toThrow = null, UnitOfWorkBehavior behavior = null)
        {
            var runner = behavior ?? new UnitOfWorkBehavior();

            var context = new TestableIncomingPhysicalMessageContext
            {
                Services = services
            };

            return runner.Invoke(context, (_, __) =>
            {
                if (toThrow != null)
                {
                    throw toThrow;
                }

                return Task.CompletedTask;
            }, default);
        }

        class UnitOfWorkThatThrowsFromEnd : IManageUnitsOfWork
        {
            public bool BeginCalled;
            public bool EndCalled;
            public Exception ExceptionThrownFromEnd = new InvalidOperationException();

            public Task Begin()
            {
                BeginCalled = true;
                return Task.CompletedTask;
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
                return Task.CompletedTask;
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
                return Task.CompletedTask;
            }

            public Task End(Exception ex = null)
            {
                ExceptionPassedToEnd = ex;
                EndCalled = true;
                return Task.CompletedTask;
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
                return Task.CompletedTask;
            }
        }

        class UnitOfWorkThatReturnsNullForEnd : IManageUnitsOfWork
        {
            public Task Begin()
            {
                return Task.CompletedTask;
            }

            public Task End(Exception ex = null)
            {
                return null;
            }
        }

        [Test]
        public async Task Verify_order()
        {
            var services = new ServiceCollection();

            var unitOfWork1 = new CountingUnitOfWork();
            var unitOfWork2 = new CountingUnitOfWork();
            var unitOfWork3 = new CountingUnitOfWork();

            services.AddTransient<IManageUnitsOfWork>(sp => unitOfWork1);
            services.AddTransient<IManageUnitsOfWork>(sp => unitOfWork2);
            services.AddTransient<IManageUnitsOfWork>(sp => unitOfWork3);

            await InvokeBehavior(services);

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
                return Task.CompletedTask;
            }

            public Task End(Exception ex = null)
            {
                EndCallCount++;
                EndCallIndex = EndCallCount;
                return Task.CompletedTask;
            }
        }

        [Test]
        public void Should_pass_exception_to_cleanup()
        {
            var services = new ServiceCollection();

            var unitOfWork = new CaptureExceptionPassedToEndUnitOfWork();
            var throwingUoW = new UnitOfWorkThatThrowsFromEnd();

            services.AddTransient<IManageUnitsOfWork>(sp => unitOfWork);
            services.AddTransient<IManageUnitsOfWork>(sp => throwingUoW);

            //since it is a single exception then it will not be an AggregateException
            Assert.That(async () => await InvokeBehavior(services), Throws.InstanceOf<InvalidOperationException>().And.SameAs(throwingUoW.ExceptionThrownFromEnd));
            Assert.AreSame(throwingUoW.ExceptionThrownFromEnd, unitOfWork.Exception);
        }

        class CaptureExceptionPassedToEndUnitOfWork : IManageUnitsOfWork
        {
            public Task Begin()
            {
                return Task.CompletedTask;
            }

            public Task End(Exception ex = null)
            {
                Exception = ex;
                return Task.CompletedTask;
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
                return Task.CompletedTask;
            }

            public Task End(Exception ex = null)
            {
                order.Add(name);
                return Task.CompletedTask;
            }
        }
    }
}