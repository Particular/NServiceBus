namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.Serialization;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using ObjectBuilder;

    [TestFixture]
    public class BehaviorChainTests
    {
        [Test]
        public void When_exception_is_thrown_stack_trace_is_trimmed()
        {
            var behaviorChain = new BehaviorChain<FakeContext>(new List<Type>
                {
                    typeof(SimpleBehavior1),
                    typeof(SimpleBehavior2),
                    typeof(BehaviorThatThrows),
                });

            var exception = Assert.Throws<FakeException>(() => behaviorChain.Invoke(new FakeContext(null)));
            var stackTraceLines = exception.StackTrace
                .Split(new [] { '\r', '\n' },StringSplitOptions.RemoveEmptyEntries);
          
            Assert.AreEqual(1, CountStringOccurrences(exception.StackTrace,".Invoke(FakeContext"),"Should be only one 'Behavior.Invoke' in the stack trace");
            Assert.IsTrue(stackTraceLines[0].Contains("BehaviorThatThrows.Invoke(FakeContext context, Action next)"),"Fist line should be the method that threw");
            Assert.IsTrue(stackTraceLines[1].Contains("BehaviorChain`1.InvokeNext(T context)"), "Second line should be the Recursive Invoke");
            Assert.AreEqual(exception.Message, "Exception Message");

        }
        static int CountStringOccurrences(string text, string pattern)
        {
            // Loop through all instances of the string 'text'.
            var count = 0;
            var i = 0;
            while ((i = text.IndexOf(pattern, i)) != -1)
            {
                i += pattern.Length;
                count++;
            }
            return count;
        }

        public class FakeContext : BehaviorContext
        {
            public FakeContext(BehaviorContext parentContext)
                : base(parentContext)
            {
                Set<IBuilder>(new FuncBuilder());
            }
        }

        public class SimpleBehavior1 : IBehavior<FakeContext>
        {
            public void Invoke(FakeContext context, Action next)
            {
                next();
            }
        }

        public class SimpleBehavior2 : IBehavior<FakeContext>
        {
            public void Invoke(FakeContext context, Action next)
            {
                next();
            }
        }

        public class BehaviorThatThrows : IBehavior<FakeContext>
        {
            public void Invoke(FakeContext context, Action next)
            {
                throw new FakeException();
            }
        }

        [Serializable]
        public class FakeException : Exception
        {
            public FakeException()
                : base("Exception Message")
            {
            }

            public FakeException(string message)
                : base(message)
            {
            }

            public FakeException(string message, Exception innerException)
                : base(message, innerException)
            {
            }

            protected FakeException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
        }
    }
}