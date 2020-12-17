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

            var expected = NotImplementedAt(Interface.IHandle, 3, 20);

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

            var expected = NotImplementedAt(Interface.IHandle, 3, 25);

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

            var expected = NotImplementedAt(Interface.IHandle, 3, 25);

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

            var expected1 = NotImplementedAt(Interface.IHandle, 3, 20);
            var expected2 = NotImplementedAt(Interface.IHandle, 3, 46);

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
    public Task Handle(MsgType message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class MsgType : ICommand {}
public class MsgType2 : ICommand {}
";

            var expected = NotImplementedAt(Interface.IHandle, 3, 66);

            return Verify(source, expected);
        }

        [Test]
        public Task SagaMissingTimeout()
        {
            var source =
@"using NServiceBus;
using System.Threading.Tasks;
public class Foo : Saga<FooData>, IAmStartedByMessages<MsgType>, IHandleTimeouts<MsgType2>
{
    public Task Handle(MsgType message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class MsgType : ICommand {}
public class MsgType2 : ICommand {}
";

            var expected = NotImplementedAt(Interface.Timeouts, 3, 66);

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
    public Task Handle(MsgType2 message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class MsgType : ICommand {}
public class MsgType2 : ICommand {}
";

            var expected = NotImplementedAt(Interface.IAmStarted, 3, 35);

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

            var noCancellation = this.NoCancellationAt(5, 17);
            var expectedTooMany = TooManyHandlesAt((5, 17), (10, 17));

            return Verify(source, noCancellation, expectedTooMany);
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

            var noCancellation = this.NoCancellationAt(5, 17);
            var expectedTooMany = TooManyHandlesAt((5, 17), (10, 17));

            return Verify(source, noCancellation, expectedTooMany);
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

            var expectedNoCancellation1 = NoCancellationAt(5, 17);
            var expectedTooMany = TooManyHandlesAt((5, 17), (10, 17), (15, 17), (20, 17));
            var expectedNoCancellation2 = NoCancellationAt(15, 17);

            return Verify(source, expectedNoCancellation1, expectedTooMany, expectedNoCancellation2);
        }

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
    public Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }
}
", Description = "Found Handle - no cancellation token")]
        public Task OnlyCancellationTokenWarning(string source)
        {
            var expected = NoCancellationAt(5, 17);

            return Verify(source, expected);
        }


        DiagnosticResult NotImplementedAt(string diagnosticId, int line, int character)
        {
            return new DiagnosticResult
            {
                Id = diagnosticId,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", line, character) }
            };
        }

        DiagnosticResult TooManyHandlesAt(params ValueTuple<int, int>[] lineCharacterPairs)
        {
            return new DiagnosticResult
            {
                Id = "NSB0005",
                Severity = DiagnosticSeverity.Error,
                Locations = lineCharacterPairs.Select(tuple => new DiagnosticResultLocation("Test0.cs", tuple.Item1, tuple.Item2)).ToArray()
            };
        }

        DiagnosticResult NoCancellationAt(int line, int character)
        {
            return new DiagnosticResult
            {
                Id = "NSB0006",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", line, character) }
            };
        }

        static class Interface
        {
            public static string IHandle = "NSB0002";
            public const string IAmStarted = "NSB0003";
            public const string Timeouts = "NSB0004";
        }

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
    public Task Handle(MsgType1 message, IMessageHandlerContext context, CancellationToken cancellationToken)
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
using NotNSB;
public class Foo : IHandleMessages<MsgType1>, IHandleMessages<MsgType2>
{
}

public class MsgType1 : ICommand {}
public class MsgType2 : ICommand {}

namespace NotNSB
{
    public interface IHandleMessages<T> {}
}
", Description = "Not NServiceBus IHandle")]

        [TestCase(@"
using NServiceBus;
public class Foo : Saga<FooData>, IAmStartedByMessages<MsgType1>, IHandleMessages<MsgType2>, IHandleTimeouts<MsgType3>
{
    public Task Handle(MsgType1 message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task Handle(MsgType2 message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task Timeout(MsgType3 message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class FooData : ContainSagaData {}
public class MsgType1 : ICommand {}
public class MsgType2 : ICommand {}
public class MsgType3 : ICommand {}
", Description = "Valid Saga example")]
        public Task NoDiagnosticIsReported(string source) => Verify(source);

        protected override DiagnosticAnalyzer GetAnalyzer() => new MustImplementIHandleMessagesAnalyzer();
    }
}
