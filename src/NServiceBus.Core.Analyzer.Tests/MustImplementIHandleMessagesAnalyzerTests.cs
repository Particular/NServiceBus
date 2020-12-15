namespace NServiceBus.Core.Analyzer.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Helpers;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    [TestFixture]
    public class MustImplementIHandleMessagesAnalyzerTests : DiagnosticVerifier
    {
        [Test]
        public Task Simple()
        {
            var source =
@"using NServiceBus;
using System.Threading.Tasks;
public class Foo : IHandleMessages<MsgType>
{
}

public class MsgType : ICommand {}
";

            var expected = NotImplementedAt(3, 20);

            return Verify(source, expected);
        }

        [Test]
        public Task HasBaseClass()
        {
            var source =
@"using NServiceBus;
using System.Threading.Tasks;
public class Foo : Bar, IHandleMessages<MsgType>
{
}

public class MsgType : ICommand {}
";

            var expected = NotImplementedAt(3, 25);

            return Verify(source, expected);
        }

        [Test]
        public Task HasWeirdWhitespace()
        {
            var source =
@"using NServiceBus;
using System.Threading.Tasks;
public class Foo : Bar, IHandleMessages    <      MsgType      >
{
}

public class MsgType : ICommand {}
";

            var expected = NotImplementedAt(3, 25);

            return Verify(source, expected);
        }

        [Test]
        public Task Missing2Implementations()
        {
            var source =
@"using NServiceBus;
using System.Threading.Tasks;
public class Foo : IHandleMessages<MsgType>, IHandleMessages<MsgType2>
{
}

public class MsgType : ICommand {}
public class MsgType2 : ICommand {}
";

            var expected1 = NotImplementedAt(3, 20);
            var expected2 = NotImplementedAt(3, 46);

            return Verify(source, expected1, expected2);
        }

        [Test]
        public Task SagaMissingIHandle()
        {
            var source =
@"using NServiceBus;
using System.Threading.Tasks;
public class Foo : Saga<FooData>, IAmStartedByMessages<MsgType>, IHandleMessages<MsgType2>
{
    public Task Handle(MsgType message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }
}

public class MsgType : ICommand {}
public class MsgType2 : ICommand {}
";

            var expected = NotImplementedAt(3, 66);

            return Verify(source, expected);
        }

        [Test]
        public Task SagaMissingIAmStartedBy()
        {
            var source =
@"using NServiceBus;
using System.Threading.Tasks;
public class Foo : Saga<FooData>, IAmStartedByMessages<MsgType>, IHandleMessages<MsgType2>
{
    public Task Handle(MsgType2 message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }
}

public class MsgType : ICommand {}
public class MsgType2 : ICommand {}
";

            var expected = NotImplementedAt(3, 35);

            return Verify(source, expected);
        }

        [Test]
        public Task TwoHandleMethods()
        {
            var source =
@"using NServiceBus;
using System.Threading.Tasks;
public class Foo : IHandleMessages<MsgType>
{
    public Task Handle(MsgType message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }

    public Task Handle(MsgType message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class MsgType : ICommand {}
";

            var expected = TooManyHandlesAt((5, 17), (10, 17));

            return Verify(source, expected);
        }

        [Test]
        public Task TwoHandleAsyncMethods()
        {
            var source =
@"using NServiceBus;
using System.Threading.Tasks;
public class Foo : IHandleMessages<MsgType>
{
    public Task HandleAsync(MsgType message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }

    public Task HandleAsync(MsgType message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class MsgType : ICommand {}
";

            var expected = TooManyHandlesAt((5, 17), (10, 17));

            return Verify(source, expected);
        }

        [Test]
        public Task All4HandleMethods()
        {
            var source =
@"using NServiceBus;
using System.Threading.Tasks;
public class Foo : IHandleMessages<MsgType>
{
    public Task Handle(MsgType message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }

    public Task Handle(MsgType message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task HandleAsync(MsgType message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }

    public Task HandleAsync(MsgType message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class MsgType : ICommand {}
";

            var expected = TooManyHandlesAt((5, 17), (10, 17), (15, 17), (20, 17));

            return Verify(source, expected);
        }









        DiagnosticResult NotImplementedAt(int line, int character)
        {
            return new DiagnosticResult
            {
                Id = "NSB0002",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", line, character) }
            };
        }

        DiagnosticResult TooManyHandlesAt(params ValueTuple<int, int>[] lineCharacterPairs)
        {
            return new DiagnosticResult
            {
                Id = "NSB0003",
                Severity = DiagnosticSeverity.Error,
                Locations = lineCharacterPairs.Select(tuple => new DiagnosticResultLocation("Test0.cs", tuple.Item1, tuple.Item2)).ToArray()
            };
        }

        [TestCase(@"
using NServiceBus;
public class Foo : IHandleMessages<TestMessage>
{
    public Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }
}
", Description = "Found Handle - no cancellation token")]

        [TestCase(@"
using NServiceBus;
public class Foo : IHandleMessages<TestMessage>
{
    public Task Handle(TestMessage message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
", Description = "Found Handle - with cancellation token")]

        [TestCase(@"
using NServiceBus;
public class Foo : IHandleMessages<TestMessage>
{
    public Task HandleAsync(TestMessage message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }
}
", Description = "Found HandleAsync - no cancellation token")]

        [TestCase(@"
using NServiceBus;
public class Foo : IHandleMessages<TestMessage>
{
    public Task HandleAsync(TestMessage message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
", Description = "Found HandleAsync - with cancellation token")]

        [TestCase(@"
using NServiceBus;
public class Foo
{
}
", Description = "No IHandleMessages<T>")]

        [TestCase(@"
using NServiceBus;
public class Foo : Bar
{
}

public class Bar {}
", Description = "Unrelated base class")]

        [TestCase(@"
using NServiceBus;
public class Foo : Bar, IMessage
{
}

public class Bar {}
", Description = "Unrelated base class and IMessage")]

        [TestCase(@"
using NServiceBus;
public class Foo : IHandleMessages<MsgType1>, IHandleMessages<MsgType2>
{
    public Task Handle(MsgType1 message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }

    public Task Handle(MsgType2 message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class MsgType1 : ICommand {}
public class MsgType2 : ICommand {}
", Description = "Implements 2 message handlers")]

        [TestCase(@"
using NServiceBus;
public class Foo : Saga<FooData>, IAmStartedByMessages<MsgType1>, IHandleMessages<MsgType2>
{
    public Task Handle(MsgType1 message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }

    public Task Handle(MsgType2 message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class FooData : ContainSagaData {}
public class MsgType1 : ICommand {}
public class MsgType2 : ICommand {}
", Description = "Valid Saga example")]
        public Task NoDiagnosticIsReported(string source) => Verify(source);

        protected override DiagnosticAnalyzer GetAnalyzer() => new MustImplementIHandleMessagesAnalyzer();
    }
}
