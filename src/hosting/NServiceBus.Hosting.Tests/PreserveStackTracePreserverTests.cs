namespace NServiceBus.Hosting.Tests
{
    using System;
    using System.Diagnostics;
    using Windows;
    using NUnit.Framework;

    [TestFixture]
    public class PreserveStackTracePreserverTests
    {

        [Test]
        public void PreservedStackTraceShouldInclude()
        {
            var preservedException = Assert.Throws<Exception>(MethodThatReThrowsInnerExceptionWithPreserve);
            Debug.WriteLine("preservedException: " + preservedException.StackTrace);
            Assert.IsTrue(preservedException.StackTrace.Contains("MethodThatThrows2"));
            Assert.IsTrue(preservedException.StackTrace.Contains("MethodThatThrows1"));
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