namespace NServiceBus.Core.Analyzer.Tests
{
    using System.Threading.Tasks;
    using Helpers;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    [TestFixture]
    public class ForwardCancellationTokenFromhandlerTests : DiagnosticVerifier
    {
        [Test]
        public Task Simple()
        {
            var source =
@"using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        return TestMethod();
    }

    static Task TestMethod(CancellationToken token = default(CancellationToken)) => Task.CompletedTask;
}
public class TestMessage : ICommand {}
";

            var expected = NotForwardedAt(8, 16);

            return Verify(source, expected);
        }

        [Test]
        public Task MethodAcceptingTokenIsOnDifferentClass()
        {
            var source =
@"using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        var thing = new Thing();
        return thing.TestMethod();
    }
}
public class Thing
{
    public Task TestMethod(CancellationToken token = default(CancellationToken)) => Task.CompletedTask;
}
public class TestMessage : ICommand {}
";

            var expected = NotForwardedAt(9, 16);

            return Verify(source, expected);
        }

        [Test]
        public Task MethodAcceptingTokenIsOnABaseClass()
        {
            var source =
@"using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        var thing = new Thing();
        return thing.TestMethod();
    }
}
public class Thing : BaseThing
{
    public override Task TestMethod()
    {
        return base.TestMethod(CancellationToken.None);
    }
}
public class BaseThing
{
    public Task TestMethod(CancellationToken token) => Task.CompletedTask;
}
public class TestMessage : ICommand {}
";

            var expected = NotForwardedAt(9, 16);

            return Verify(source, expected);
        }

        [Test]
        public Task MethodAcceptingTokenIsOnASuperClass()
        {
            var source =
@"using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        var thing = new Thing();
        return thing.TestMethod();
    }
}
public class Thing : BaseThing
{
    public Task TestMethod(CancellationToken token) => Task.CompletedTask;
}
public class BaseThing
{
    public Task TestMethod() => Task.CompletedTask;
}
public class TestMessage : ICommand {}
";

            var expected = NotForwardedAt(9, 16);

            return Verify(source, expected);
        }

        [Test]
        public Task LotsOfParameters()
        {
            var source =
@"using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        return TestMethod(true, false, true false, true);
    }

    static Task TestMethod(bool p1, bool p2, bool p3, bool p4, bool p5, CancellationToken token = default(CancellationToken)) => Task.CompletedTask;
}
public class TestMessage : ICommand {}
";

            var expected = NotForwardedAt(8, 16);

            return Verify(source, expected);
        }

        [Test]
        public Task LotsOfOverloadsThatAllTakeToken()
        {
            var source =
@"using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        return TestMethod(true);
        return TestMethod(true, false);
        return TestMethod(true, false, true);
        return TestMethod(true, false, true false);
        return TestMethod(true, false, true false, true);
    }

    static Task TestMethod(bool p1, CancellationToken token = default(CancellationToken)) => Task.CompletedTask;
    static Task TestMethod(bool p1, bool p2, CancellationToken token = default(CancellationToken)) => Task.CompletedTask;
    static Task TestMethod(bool p1, bool p2, bool p3, CancellationToken token = default(CancellationToken)) => Task.CompletedTask;
    static Task TestMethod(bool p1, bool p2, bool p3, bool p4, CancellationToken token = default(CancellationToken)) => Task.CompletedTask;
    static Task TestMethod(bool p1, bool p2, bool p3, bool p4, bool p5, CancellationToken token = default(CancellationToken)) => Task.CompletedTask;
}
public class TestMessage : ICommand {}
";
            var expecteds = new[]
            {
                NotForwardedAt(8, 16),
                NotForwardedAt(9, 16),
                NotForwardedAt(10, 16),
                NotForwardedAt(11, 16),
                NotForwardedAt(12, 16),
            };

            return Verify(source, expecteds);
        }

        [Test]
        public Task LotsOfOverloadsButOnlyOneTakesToken()
        {
            var source =
@"using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        return TestMethod(true);
        return TestMethod(true, false);
        return TestMethod(true, false, true);
        return TestMethod(true, false, true false);
        return TestMethod(true, false, true false, true);
    }

    static Task TestMethod(bool p1) => Task.CompletedTask;
    static Task TestMethod(bool p1, bool p2) => Task.CompletedTask;
    static Task TestMethod(bool p1, bool p2, bool p3) => Task.CompletedTask;
    static Task TestMethod(bool p1, bool p2, bool p3, bool p4) => Task.CompletedTask;
    static Task TestMethod(bool p1, bool p2, bool p3, bool p4, bool p5, CancellationToken token = default(CancellationToken)) => Task.CompletedTask;
}
public class TestMessage : ICommand {}
";
            var expecteds = new[]
            {
                NotForwardedAt(8, 16),
                NotForwardedAt(9, 16),
                NotForwardedAt(10, 16),
                NotForwardedAt(11, 16),
                NotForwardedAt(12, 16),
            };

            return Verify(source, expecteds);
        }


        DiagnosticResult NotForwardedAt(int line, int character)
        {
            return new DiagnosticResult
            {
                Id = "NSB0007",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", line, character) }
            };
        }

        [Test]
        public Task NoDiagnosticWhenTokenPassedToStaticMethod()
        {
            return Verify(@"
using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        return TestMethod(context.CancellationToken);
        return Foo.TestMethod(context.CancellationToken);
    }

    static Task TestMethod(CancellationToken token = default(CancellationToken)) => Task.CompletedTask;
}
public class TestMessage : ICommand {}
");
        }

        [Test]
        public Task NoDiagnosticWhenStaticMethodDoesNotSupportToken()
        {
            return Verify(@"
using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        return TestMethod(true);
        return Foo.TestMethod(true);
    }

    static Task TestMethod(bool value, string optionalParam = null) => Task.CompletedTask;
}
public class TestMessage : ICommand {}
");
        }

        [Test]
        public Task NoDiagnosticWhenTokenPassedToExternalStaticMethod()
        {
            return Verify(@"
using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        return OtherClass.TestMethod(context.CancellationToken);
    }
}
public static class OtherClass
{
    public static Task TestMethod(CancellationToken token = default(CancellationToken)) => Task.CompletedTask;
}
public class TestMessage : ICommand {}
");
        }

        [Test]
        public Task NoDiagnosticWhenExternalStaticMethodDoesNotSupportToken()
        {
            return Verify(@"
using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        return OtherClass.TestMethod(true);
    }
}
public static class OtherClass
{
    public static Task TestMethod(CancellationToken token = default(CancellationToken)) => Task.CompletedTask;
}
public class TestMessage : ICommand {}
");
        }

        [Test]
        public Task NoDiagnosticWhenTokenPassedToExternalStaticMethodWithStaticUsing()
        {
            return Verify(@"
using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
using static OtherClass;
public class Foo : IHandleMessages<TestMessage>
{
    public Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        return TestMethod(context.CancellationToken);
    }
}
public static class OtherClass
{
    public static Task TestMethod(CancellationToken token = default(CancellationToken)) => Task.CompletedTask;
}
public class TestMessage : ICommand {}
");
        }

        [Test]
        public Task NoDiagnosticWhenExternalStaticMethodDoesNotSupportTokenWithStaticUsing()
        {
            return Verify(@"
using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
using static OtherClass;
public class Foo : IHandleMessages<TestMessage>
{
    public Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        return TestMethod(true);
    }
}
public static class OtherClass
{
    public static Task TestMethod(CancellationToken token = default(CancellationToken)) => Task.CompletedTask;
}
public class TestMessage : ICommand {}
");
        }

        [Test]
        public Task NoDiagnosticWhenTokenPassedToMemberMethod()
        {
            return Verify(@"
using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        return TestMethod(context.CancellationToken);
        return this.TestMethod(context.CancellationToken);
    }

    Task TestMethod(CancellationToken token = default(CancellationToken)) => Task.CompletedTask;
}
public class TestMessage : ICommand {}
");
        }

        [Test]
        public Task NoDiagnosticWhenMemberMethodDoesNotSupportToken()
        {
            return Verify(@"
using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        return TestMethod(true);
        return this.TestMethod(true);
    }

    Task TestMethod(bool value, string optionalParam = null) => Task.CompletedTask;
}
public class TestMessage : ICommand {}
");
        }

        protected override DiagnosticAnalyzer GetAnalyzer() => new ForwardCancellationTokenFromHandlerAnalyzer();
    }
}
