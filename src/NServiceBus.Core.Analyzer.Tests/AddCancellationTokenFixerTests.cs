namespace NServiceBus.Core.Analyzer.Tests
{
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NServiceBus.Core.Analyzer.Tests.Helpers;
    using NUnit.Framework;
    using System.Threading.Tasks;

    [TestFixture]
    public class AddCancellationTokenFixerTests : CodeFixVerifier
    {
        [Test]
        public async Task OnHandle()
        {
            var test =
@"using NServiceBus;
using System.Threading.Tasks;

public class Foo : IHandleMessages<MsgType>
{
    public async Task Handle(MsgType message, IMessageHandlerContext context)
    {
        await Task.Delay(1);
    }
}

public class MsgType : ICommand {}
";

            string fixedTest =
@"using NServiceBus;
using System.Threading.Tasks;

public class Foo : IHandleMessages<MsgType>
{
    public async Task Handle(MsgType message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
        await Task.Delay(1);
    }
}

public class MsgType : ICommand {}
";
            await VerifyFix(test, fixedTest, allowNewCompilerDiagnostics: true);
        }

        [Test]
        public async Task OnIAmStarted()
        {
            var test =
@"using NServiceBus;
using System.Threading.Tasks;

public class Foo : IAmStartedByMessages<MsgType>
{
    public async Task Handle(MsgType message, IMessageHandlerContext context)
    {
        await Task.Delay(1);
    }
}

public class MsgType : ICommand {}
";

            string fixedTest =
@"using NServiceBus;
using System.Threading.Tasks;

public class Foo : IAmStartedByMessages<MsgType>
{
    public async Task Handle(MsgType message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
        await Task.Delay(1);
    }
}

public class MsgType : ICommand {}
";
            await VerifyFix(test, fixedTest, allowNewCompilerDiagnostics: true);
        }

        [Test]
        public async Task OnTimeout()
        {
            var test =
@"using NServiceBus;
using System.Threading.Tasks;

public class Foo : IHandleTimeouts<MsgType>
{
    public async Task Timeout(MsgType message, IMessageHandlerContext context)
    {
        await Task.Delay(1);
    }
}

public class MsgType : ICommand {}
";

            string fixedTest =
@"using NServiceBus;
using System.Threading.Tasks;

public class Foo : IHandleTimeouts<MsgType>
{
    public async Task Timeout(MsgType message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
        await Task.Delay(1);
    }
}

public class MsgType : ICommand {}
";
            await VerifyFix(test, fixedTest, allowNewCompilerDiagnostics: true);
        }














        [Test]
        public async Task OnHandleAsync()
        {
            var test =
@"using NServiceBus;
using System.Threading.Tasks;

public class Foo : IHandleMessages<MsgType>
{
    public async Task HandleAsync(MsgType message, IMessageHandlerContext context)
    {
        await Task.Delay(1);
    }
}

public class MsgType : ICommand {}
";

            string fixedTest =
@"using NServiceBus;
using System.Threading.Tasks;

public class Foo : IHandleMessages<MsgType>
{
    public async Task HandleAsync(MsgType message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
        await Task.Delay(1);
    }
}

public class MsgType : ICommand {}
";
            await VerifyFix(test, fixedTest, allowNewCompilerDiagnostics: true);
        }

        [Test]
        public async Task OnIAmStartedAsync()
        {
            var test =
@"using NServiceBus;
using System.Threading.Tasks;

public class Foo : IAmStartedByMessages<MsgType>
{
    public async Task HandleAsync(MsgType message, IMessageHandlerContext context)
    {
        await Task.Delay(1);
    }
}

public class MsgType : ICommand {}
";

            string fixedTest =
@"using NServiceBus;
using System.Threading.Tasks;

public class Foo : IAmStartedByMessages<MsgType>
{
    public async Task HandleAsync(MsgType message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
        await Task.Delay(1);
    }
}

public class MsgType : ICommand {}
";
            await VerifyFix(test, fixedTest, allowNewCompilerDiagnostics: true);
        }

        [Test]
        public async Task OnTimeoutAsync()
        {
            var test =
@"using NServiceBus;
using System.Threading.Tasks;

public class Foo : IHandleTimeouts<MsgType>
{
    public async Task TimeoutAsync(MsgType message, IMessageHandlerContext context)
    {
        await Task.Delay(1);
    }
}

public class MsgType : ICommand {}
";

            string fixedTest =
@"using NServiceBus;
using System.Threading.Tasks;

public class Foo : IHandleTimeouts<MsgType>
{
    public async Task TimeoutAsync(MsgType message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
        await Task.Delay(1);
    }
}

public class MsgType : ICommand {}
";
            await VerifyFix(test, fixedTest, allowNewCompilerDiagnostics: true);
        }

        protected override DiagnosticAnalyzer GetAnalyzer() => new MustImplementIHandleMessagesAnalyzer();

        protected override CodeFixProvider GetCodeFixProvider() => new AddCancellationTokenFixer();
    }
}
