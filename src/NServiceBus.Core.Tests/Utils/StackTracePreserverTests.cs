namespace NServiceBus.Core.Tests.Utils
{
    using System;
    using System.Runtime.CompilerServices;
    using NUnit.Framework;
    using NServiceBus.Utils;

    [TestFixture]
    public class StackTracePreserverTests
    {
        [Test]
        public void PreservedStackTraceShouldInclude()
        {
            //Note the NoInlining below is to prevent the JIT from optimizing out the stack trace
            var preservedException = Assert.Throws<Exception>(MethodThatReThrowsInnerExceptionWithPreserve);
            var actual = "actual: " + preservedException.StackTrace;
            Assert.IsTrue(preservedException.StackTrace.Contains("MethodThatThrows2"), actual);
            Assert.IsTrue(preservedException.StackTrace.Contains("MethodThatThrows1"), actual);
        }
        [Test]
        public void ShouldNotThrowWhenHandlingNonSerializableExceptions()
        {
            new NonSerializableException().PreserveStackTrace();
        }

        public class NonSerializableException : Exception
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        void MethodThatReThrowsInnerExceptionWithPreserve()
        {
            try
            {
                MethodThatThrowsWithInnerException();
            }
            catch (Exception exception)
            {
                var innerException = exception.InnerException;
                innerException.PreserveStackTrace();
                throw innerException;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        void MethodThatThrowsWithInnerException()
        {
            try
            {
                MethodThatThrows1();
            }
            catch (Exception exception)
            {
                throw new Exception("bar", exception);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        void MethodThatThrows1()
        {
            MethodThatThrows2();
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        void MethodThatThrows2()
        {
            throw new Exception("Foo");
        }
    }
}