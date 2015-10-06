namespace NServiceBus.Core.Tests.Utils
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using NServiceBus.Faults;
    using NUnit.Framework;

    [TestFixture]
    public class ExceptionHeaderHelperTests
    {
        [Test]
        public void VerifyHeadersAreSet()
        {
            var exception = GetAnException();
            var dictionary = new Dictionary<string, string>();

            var failedQueue = new Address("TheErrorQueue", "TheErrorQueueMachine");
            ExceptionHeaderHelper.SetExceptionHeaders(dictionary, exception, failedQueue, "The reason", false);
            Assert.AreEqual("The reason", dictionary["NServiceBus.ExceptionInfo.Reason"]);
            Assert.AreEqual("System.AggregateException", dictionary["NServiceBus.ExceptionInfo.ExceptionType"]);
            var stackTrace = dictionary["NServiceBus.ExceptionInfo.StackTrace"];
            Assert.IsTrue(stackTrace.StartsWith(@"System.AggregateException: My Exception ---> System.Exception: My Inner Exception
   at NServiceBus.Core.Tests.Utils.ExceptionHeaderHelperTests.MethodThatThrows2() in "));
            Assert.AreEqual("TheErrorQueue@TheErrorQueueMachine", dictionary[FaultsHeaderKeys.FailedQ]);
            Assert.IsTrue(dictionary.ContainsKey("NServiceBus.TimeOfFailure"));

            Assert.AreEqual("System.Exception", dictionary["NServiceBus.ExceptionInfo.InnerExceptionType"]);
            Assert.AreEqual("A fake help link", dictionary["NServiceBus.ExceptionInfo.HelpLink"]);
            Assert.AreEqual("NServiceBus.Core.Tests", dictionary["NServiceBus.ExceptionInfo.Source"]);
        }



        [Test]
        public void VerifyLegacyHeadersAreSet()
        {
            var exception = GetAnException();
            var dictionary = new Dictionary<string, string>();

            var failedQueue = new Address("TheErrorQueue", "TheErrorQueueMachine");
            ExceptionHeaderHelper.SetExceptionHeaders(dictionary, exception, failedQueue, "The reason", true);
            Assert.AreEqual("The reason", dictionary["NServiceBus.ExceptionInfo.Reason"]);
            Assert.AreEqual("System.AggregateException", dictionary["NServiceBus.ExceptionInfo.ExceptionType"]);
            var stackTrace = dictionary["NServiceBus.ExceptionInfo.StackTrace"];
            Assert.IsTrue(stackTrace.StartsWith(@"   at NServiceBus.Core.Tests.Utils.ExceptionHeaderHelperTests.MethodThatThrows1() in "));
            Assert.AreEqual("TheErrorQueue@TheErrorQueueMachine", dictionary[FaultsHeaderKeys.FailedQ]);
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
        public void VerifyDataIsSet()
        {
            var exception = GetAnException();
            exception.Data["TestKey"] = "MyValue";

            var dictionary = new Dictionary<string, string>();

            var failedQueue = new Address("TheErrorQueue", "TheErrorQueueMachine");
            ExceptionHeaderHelper.SetExceptionHeaders(dictionary, exception, failedQueue, "The reason", false);

            Assert.AreEqual("MyValue", dictionary["NServiceBus.ExceptionInfo.Data.TestKey"]);
        }

        class NullDataException : Exception
        {
            public override IDictionary Data
            {
// ReSharper disable once AssignNullToNotNullAttribute
                get { return null; }
            }
        }

        [Test]
        public void VerifyNullDataDoesNotThrow()
        {
            var exception = new NullDataException();
            var dictionary = new Dictionary<string, string>();

            var failedQueue = new Address("TheErrorQueue", "TheErrorQueueMachine");
            ExceptionHeaderHelper.SetExceptionHeaders(dictionary, exception, failedQueue, "The reason", false);
        }

        [Test]
        public void ExceptionMessageIsTruncated()
        {
            var exception = new Exception(new string('x', (int)Math.Pow(2, 15)));
            var dictionary = new Dictionary<string, string>();

            ExceptionHeaderHelper.SetExceptionHeaders(dictionary, exception, new Address("queue1", "machine1"), "reason1", false);

            Assert.AreEqual((int)Math.Pow(2, 14), dictionary["NServiceBus.ExceptionInfo.Message"].Length);
        }
    }
}