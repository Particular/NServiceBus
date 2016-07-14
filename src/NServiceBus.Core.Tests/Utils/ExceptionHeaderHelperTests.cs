namespace NServiceBus.Core.Tests.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using NUnit.Framework;

    [TestFixture]
    public class ExceptionHeaderHelperTests
    {
        [Test]
        public void VerifyHeadersAreSet()
        {
            var exception = GetAnException();
            var dictionary = new Dictionary<string, string>();

            ExceptionHeaderHelper.SetExceptionHeaders(dictionary, exception);

            Assert.AreEqual("System.AggregateException", dictionary["NServiceBus.ExceptionInfo.ExceptionType"]);
            Assert.AreEqual(exception.ToString(), dictionary["NServiceBus.ExceptionInfo.StackTrace"]);
            Assert.IsTrue(dictionary.ContainsKey("NServiceBus.TimeOfFailure"));

            Assert.AreEqual("System.Exception", dictionary["NServiceBus.ExceptionInfo.InnerExceptionType"]);
            Assert.AreEqual("A fake help link", dictionary["NServiceBus.ExceptionInfo.HelpLink"]);
            Assert.AreEqual("NServiceBus.Core.Tests", dictionary["NServiceBus.ExceptionInfo.Source"]);
        }

        Exception GetAnException()
        {
            try
            {
                MethodThatThrows1();
            }
            catch (Exception e)
            {
                return e;
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void MethodThatThrows1()
        {
            try
            {
                MethodThatThrows2();
            }
            catch (Exception exception)
            {
                throw new AggregateException("My Exception", exception) { HelpLink = "A fake help link" };
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        void MethodThatThrows2()
        {
            throw new Exception("My Inner Exception");
        }

        [Test]
        public void ExceptionMessageIsTruncated()
        {
            var exception = new Exception(new string('x', (int)Math.Pow(2, 15)));
            var dictionary = new Dictionary<string, string>();

            ExceptionHeaderHelper.SetExceptionHeaders(dictionary, exception);

            Assert.AreEqual((int)Math.Pow(2, 14), dictionary["NServiceBus.ExceptionInfo.Message"].Length);
        }
    }
}