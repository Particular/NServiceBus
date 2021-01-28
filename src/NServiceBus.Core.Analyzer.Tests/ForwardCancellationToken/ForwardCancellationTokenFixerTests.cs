namespace NServiceBus.Core.Analyzer.Tests
{
    using System.Threading.Tasks;
    using Helpers;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    [TestFixture]
    public class ForwardCancellationTokenFixerTests : CodeFixVerifier
    {
        [Test]
        public Task Simple()
        {
            var test = @"
using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        return TestMethod();
    }

    static Task TestMethod(CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
}
public class TestMessage : ICommand {}
";

            var fixedTest = @"
using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        return TestMethod(context.CancellationToken);
    }

    static Task TestMethod(CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
}
public class TestMessage : ICommand {}
";

            return VerifyFix(test, fixedTest);
        }

        [Test]
        public Task NonStandardContextVaraibleName()
        {
            var test = @"
using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public Task Handle(TestMessage message, IMessageHandlerContext iRenamedItCuzICan)
    {
        return TestMethod();
    }

    static Task TestMethod(CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
}
public class TestMessage : ICommand {}
";

            var fixedTest = @"
using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public Task Handle(TestMessage message, IMessageHandlerContext iRenamedItCuzICan)
    {
        return TestMethod(iRenamedItCuzICan.CancellationToken);
    }

    static Task TestMethod(CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
}
public class TestMessage : ICommand {}
";

            return VerifyFix(test, fixedTest);
        }

        [Test]
        public Task OverloadsAndThis()
        {
            var test = @"
using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public async Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        await TestMethod();
        await this.TestMethod();
    }

    Task TestMethod() { return Task.CompletedTask; }
    Task TestMethod(CancellationToken token) { return Task.CompletedTask; }
}
public class TestMessage : ICommand {}
";

            var fixedTest = @"
using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public async Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        await TestMethod(context.CancellationToken);
        await this.TestMethod(context.CancellationToken);
    }

    Task TestMethod() { return Task.CompletedTask; }
    Task TestMethod(CancellationToken token) { return Task.CompletedTask; }
}
public class TestMessage : ICommand {}
";

            return VerifyFix(test, fixedTest);
        }

        protected override DiagnosticAnalyzer GetAnalyzer() => new ForwardCancellationTokenAnalyzer();

        protected override CodeFixProvider GetCodeFixProvider() => new ForwardCancellationTokenFixer();
    }
}