namespace NServiceBus.Hosting.Tests
{
    using System;
    using Windows;
    using NUnit.Framework;

    [TestFixture]
    public class PreserveStackTracePreserverTests
    {

        [Test]
        public void PreservedStackTraceShouldInclude()
        {
            var preservedException = Assert.Throws<Exception>(MethodThatReThrowsInnerExceptionWithPreserve);
            var actual = "actual: " + preservedException.StackTrace;
            Assert.IsTrue(preservedException.StackTrace.Contains("MethodThatThrows2"), actual);
            Assert.IsTrue(preservedException.StackTrace.Contains("MethodThatThrows1"), actual);
        }

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
        void MethodThatThrows1()
        {
            MethodThatThrows2();
        }
        void MethodThatThrows2()
        {
            throw new Exception("Foo");
        }
    }
}