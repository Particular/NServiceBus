namespace NServiceBus.Core.Analyzer.Tests
{
    using System.Threading.Tasks;
    using Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class ForwardCancellationTokenFixerTests : CodeFixTestFixture<ForwardCancellationTokenAnalyzer, ForwardCancellationTokenFixer>
    {
        [Test]
        public Task Simple()
        {
            var original =
@"using NServiceBus;
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
public class TestMessage : ICommand {}";

            var expected =
@"using NServiceBus;
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
public class TestMessage : ICommand {}";

            return Assert(original, expected);
        }

        [Test]
        public Task NonStandardContextVariableName()
        {
            var original =
@"using NServiceBus;
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
public class TestMessage : ICommand {}";

            var expected =
@"using NServiceBus;
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
public class TestMessage : ICommand {}";

            return Assert(original, expected);
        }

        [Test]
        public Task OverloadsAndThis()
        {
            var original =
@"using NServiceBus;
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
public class TestMessage : ICommand {}";

            var expected =
@"using NServiceBus;
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
public class TestMessage : ICommand {}";

            return Assert(original, expected);
        }

        [Test]
        public Task DontMessUpGenericTypeParams()
        {
            var original =
@"using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public async Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        int answer1 = await TestMethod(42);
        int answer2 = await TestMethod<int>(42);
        string msg1 = await TestMethod(""Hello"");
        string msg2 = await TestMethod<string>(""World"");
    }

    Task<T> TestMethod<T>(T value, CancellationToken token = default(CancellationToken)) { return Task.FromResult(value); }
}
public class TestMessage : ICommand {}";

            var expected =
@"using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public async Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        int answer1 = await TestMethod(42, context.CancellationToken);
        int answer2 = await TestMethod<int>(42, context.CancellationToken);
        string msg1 = await TestMethod(""Hello"", context.CancellationToken);
        string msg2 = await TestMethod<string>(""World"", context.CancellationToken);
    }

    Task<T> TestMethod<T>(T value, CancellationToken token = default(CancellationToken)) { return Task.FromResult(value); }
}
public class TestMessage : ICommand {}";

            return Assert(original, expected);
        }

        [Test]
        public Task DontMessUpTrivia()
        {
            var original =
@"using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public async Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        await TestMethod(1, 2, // comment
                         3, /*comment*/      4, // comment
                         //comment
                         5);
    }

    Task TestMethod(int a, int b, int c, int d, int e, CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
}
public class TestMessage : ICommand {}";

            var expected =
@"using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public async Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        await TestMethod(1, 2, // comment
                         3, /*comment*/      4, // comment
                         //comment
                         5, context.CancellationToken);
    }

    Task TestMethod(int a, int b, int c, int d, int e, CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
}
public class TestMessage : ICommand {}";

            return Assert(original, expected);
        }

        [Test]
        public Task MultipleOptionalParameters()
        {
            var original =
@"using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public async Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        await TestMethod(1);
    }

    Task TestMethod(int a, int b = 0, int c = 1, int d = 2, CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
}
public class TestMessage : ICommand {}";

            var expected =
@"using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public async Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        await TestMethod(1, token: context.CancellationToken);
    }

    Task TestMethod(int a, int b = 0, int c = 1, int d = 2, CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
}
public class TestMessage : ICommand {}";

            return Assert(original, expected);
        }
    }
}
