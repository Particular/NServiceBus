namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Core.Tests;
    using NServiceBus.Transports;
    using NUnit.Framework;
    using ObjectBuilder;
    using Pipeline.Contexts;
    using UnitOfWork;

    [TestFixture]
    public class UnitOfWorkBehaviorTests
    {
        [Test]
        public void When_first_throw_second_is_cleaned_up()
        {
            var builder = new FuncBuilder();

            var unitOfWorkThatThrowsFromEnd = new UnitOfWorkThatThrowsFromEnd();
            var unitOfWork = new UnitOfWork();

            builder.Register<IManageUnitsOfWork>(() => unitOfWorkThatThrowsFromEnd);
            builder.Register<IManageUnitsOfWork>(() => unitOfWork);

            //since it is a single exception then it will not be an AggregateException 
            Assert.Throws<InvalidOperationException>(() => InvokeBehavior(builder));
            Assert.IsTrue(unitOfWorkThatThrowsFromEnd.BeginCalled);
            Assert.IsTrue(unitOfWorkThatThrowsFromEnd.EndCalled);
            Assert.IsTrue(unitOfWork.BeginCalled);
            Assert.IsTrue(unitOfWork.EndCalled);
        }

        [Test]
        public void Should_append_end_exception_to_rethrow()
        {
            var builder = new FuncBuilder();

            var unitOfWork = new UnitOfWorkThatThrowsFromEnd();

            builder.Register<IManageUnitsOfWork>(() => unitOfWork);
            //since it is a single exception then it will not be an AggregateException 
            var exception = Assert.Throws<InvalidOperationException>(() => InvokeBehavior(builder));

            Assert.AreSame(unitOfWork.ExceptionThrownFromEnd, exception);
        }

        [Test]
        public void Should_not_invoke_end_if_begin_was_not_invoked()
        {

            var builder = new FuncBuilder();

            var unitOfWorkThatThrowsFromBegin = new UnitOfWorkThatThrowsFromBegin();
            var unitOfWork = new UnitOfWork();


            builder.Register<IManageUnitsOfWork>(() => unitOfWorkThatThrowsFromBegin);
            builder.Register<IManageUnitsOfWork>(() => unitOfWork);
            
            //since it is a single exception then it will not be an AggregateException 
            Assert.Throws<InvalidOperationException>(() => InvokeBehavior(builder));
            Assert.False(unitOfWork.EndCalled);

        }

        [Test]
        public void Should_pass_exceptions_to_the_uow_end()
        {
            var builder = new FuncBuilder();

            var unitOfWork = new UnitOfWork();


            builder.Register<IManageUnitsOfWork>(() => unitOfWork);

            var ex = new Exception("Handler failed");
            //since it is a single exception then it will not be an AggregateException 
            Assert.Throws<Exception>(() =>
            {                
                InvokeBehavior(builder,ex);
            });
            Assert.AreSame(ex, unitOfWork.ExceptionPassedToEnd );

        }

        [Test]
        public void Should_invoke_ends_in_reverse_order_of_the_begins()
        {
            var builder = new FuncBuilder();

            var order = new List<string>();
            var firstUnitOfWork = new OrderAwareUnitOfWork("first", order);
            var secondUnitOfWork = new OrderAwareUnitOfWork("second", order);


            builder.Register<IManageUnitsOfWork>(() => firstUnitOfWork);
            builder.Register<IManageUnitsOfWork>(() => secondUnitOfWork);

            InvokeBehavior(builder);

            Assert.AreEqual("first", order[0]);
            Assert.AreEqual("second", order[1]);
            Assert.AreEqual("second", order[2]);
            Assert.AreEqual("first", order[3]);
        }
        [Test]
        public void Should_call_all_end_even_if_one_or_more_of_them_throws()
        {
            var builder = new FuncBuilder();

            var unitOfWorkThatThrows = new UnitOfWorkThatThrowsFromEnd();
            var unitOfWork= new UnitOfWork();

            builder.Register<IManageUnitsOfWork>(() => unitOfWorkThatThrows);
            builder.Register<IManageUnitsOfWork>(() => unitOfWork);

            Assert.Throws<InvalidOperationException>(() => InvokeBehavior(builder));

            Assert.True(unitOfWork.EndCalled);
        }

        [Test]
        public void Should_invoke_ends_on_all_begins_that_was_called_even_when_begin_throws()
        {
            var builder = new FuncBuilder();

            var normalUnitOfWork = new UnitOfWork();
            var unitOfWorkThatThrows = new UnitOfWorkThatThrowsFromBegin();
            var unitOfWorkThatIsNeverCalled = new UnitOfWork();

            builder.Register<IManageUnitsOfWork>(() => normalUnitOfWork);
            builder.Register<IManageUnitsOfWork>(() => unitOfWorkThatThrows);
            builder.Register<IManageUnitsOfWork>(() => unitOfWorkThatIsNeverCalled);

            Assert.Throws<InvalidOperationException>(() => InvokeBehavior(builder));

            Assert.True(normalUnitOfWork.EndCalled);
            Assert.True(unitOfWorkThatThrows.EndCalled);
            Assert.False(unitOfWorkThatIsNeverCalled.EndCalled);
        }

        public void InvokeBehavior(IBuilder builder,Exception toThrow = null)
        {
            var runner = new UnitOfWorkBehavior();

            var context = new PhysicalMessageProcessingStageBehavior.Context(new TransportReceiveContext(new IncomingMessage("fakeId",new Dictionary<string, string>(),new MemoryStream() ), new IncomingContext(new RootContext(builder))));

            runner.Invoke(context, () =>
            {
                if (toThrow != null)
                {
                    throw toThrow;
                }
            });

        }



        public class UnitOfWorkThatThrowsFromEnd : IManageUnitsOfWork
        {
            public bool BeginCalled;
            public bool EndCalled;
            public Exception ExceptionThrownFromEnd = new InvalidOperationException();

            public void Begin()
            {
                BeginCalled = true;
            }

            public void End(Exception ex = null)
            {
                EndCalled = true;
                throw ExceptionThrownFromEnd;
            }

        }
        public class UnitOfWorkThatThrowsFromBegin : IManageUnitsOfWork
        {
            public bool EndCalled;
            public Exception ExceptionThrownFromEnd = new InvalidOperationException();

            public void Begin()
            {
                throw ExceptionThrownFromEnd;
            }

            public void End(Exception ex = null)
            {
                EndCalled = true;
            }

        }

        public class UnitOfWork : IManageUnitsOfWork
        {
            public bool BeginCalled;
            public bool EndCalled;
            public Exception ExceptionPassedToEnd;
            public void Begin()
            {
                BeginCalled = true;
            }

            public void End(Exception ex = null)
            {
                ExceptionPassedToEnd = ex;
                EndCalled = true;
            }
        }

        [Test]
        public void Verify_order()
        {
            var builder = new FuncBuilder();

            var unitOfWork1 = new CountingUnitOfWork();
            var unitOfWork2 = new CountingUnitOfWork();
            var unitOfWork3 = new CountingUnitOfWork();

            builder.Register<IManageUnitsOfWork>(() => unitOfWork1);
            builder.Register<IManageUnitsOfWork>(() => unitOfWork2);
            builder.Register<IManageUnitsOfWork>(() => unitOfWork3);

            InvokeBehavior(builder);

            Assert.AreEqual(1, unitOfWork1.BeginCallIndex);
            Assert.AreEqual(2, unitOfWork2.BeginCallIndex);
            Assert.AreEqual(3, unitOfWork3.BeginCallIndex);
            Assert.AreEqual(3, unitOfWork1.EndCallIndex);
            Assert.AreEqual(2, unitOfWork2.EndCallIndex);
            Assert.AreEqual(1, unitOfWork3.EndCallIndex);
        }

        public class CountingUnitOfWork : IManageUnitsOfWork
        {
            static int BeginCallCount;
            static int EndCallCount;
            public int EndCallIndex;
            public int BeginCallIndex;

            public void Begin()
            {
                BeginCallCount++;
                BeginCallIndex = BeginCallCount;
            }
            public void End(Exception ex = null)
            {
                EndCallCount++;
                EndCallIndex = EndCallCount;
            }
        }

        [Test]
        public void Should_pass_exception_to_cleanup()
        {
            var builder = new FuncBuilder();

            var unitOfWork = new CaptureExceptionPassedToEndUnitOfWork();
            var throwingUoW = new UnitOfWorkThatThrowsFromEnd();

            builder.Register<IManageUnitsOfWork>(() => unitOfWork);
            builder.Register<IManageUnitsOfWork>(() => throwingUoW);

            //since it is a single exception then it will not be an AggregateException 
            var exception = Assert.Throws<InvalidOperationException>(() => InvokeBehavior(builder));

            Assert.AreSame(throwingUoW.ExceptionThrownFromEnd, unitOfWork.Exception);
            Assert.AreSame(throwingUoW.ExceptionThrownFromEnd, exception);
        }

        public class CaptureExceptionPassedToEndUnitOfWork : IManageUnitsOfWork
        {
            public void Begin()
            {
            }
            public void End(Exception ex = null)
            {
                Exception = ex;
            }
            public Exception Exception;
        }


        public class OrderAwareUnitOfWork : IManageUnitsOfWork
        {
            readonly string name;
            readonly List<string> order;

            public OrderAwareUnitOfWork(string name, List<string> order)
            {
                this.name = name;
                this.order = order;
            }

            public void Begin()
            {
                order.Add(name);
            }

            public void End(Exception ex = null)
            {
                order.Add(name);
            }
        }
    }

}