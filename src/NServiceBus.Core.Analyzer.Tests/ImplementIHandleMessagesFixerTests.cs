namespace NServiceBus.Core.Analyzer.Tests
{
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NServiceBus.Core.Analyzer.Tests.Helpers;
    using NUnit.Framework;
    using System.Threading.Tasks;

    [TestFixture]
    public class ImplementIHandleMessagesFixerTests : CodeFixVerifier
    {
        [Test]
        public async Task SimpleTest()
        {
            var test =
@"using NServiceBus;
using System.Threading.Tasks;

public class Foo : IHandleMessages<MsgType>
{
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
    }
}

public class MsgType : ICommand {}
";
            await VerifyFix(test, fixedTest, allowNewCompilerDiagnostics: true);
        }

        [Test]
        public async Task KeepExistingMethods()
        {
            string test =
@"using NServiceBus;
using System.Threading.Tasks;

public class C : IHandleMessages<Message>
{
    private void Foo() {}

    private void Bar() {}
}";

            string fixTest =
@"using NServiceBus;
using System.Threading.Tasks;

public class C : IHandleMessages<Message>
{
    private void Foo() {}

    private void Bar() {}

    public async Task Handle(Message message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
    }
}";

            await VerifyFix(test, fixTest, allowNewCompilerDiagnostics: true);
        }

        [Test]
        public async Task SagaIHandle()
        {
            string test =
@"using NServiceBus;
using System.Threading.Tasks;

public class MySaga : Saga<MyData>, IHandleMessages<Message>
{
}

public class MyData : ContainSagaData {}";

            string fixTest =
@"using NServiceBus;
using System.Threading.Tasks;

public class MySaga : Saga<MyData>, IHandleMessages<Message>
{
    public async Task Handle(Message message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
    }
}

public class MyData : ContainSagaData {}";

            await VerifyFix(test, fixTest, allowNewCompilerDiagnostics: true);
        }

        [Test]
        public async Task SagaIAmStarted()
        {
            string test =
@"using NServiceBus;
using System.Threading.Tasks;

public class MySaga : Saga<MyData>, IAmStartedByMessages<Message>
{
}

public class MyData : ContainSagaData {}";

            string fixTest =
@"using NServiceBus;
using System.Threading.Tasks;

public class MySaga : Saga<MyData>, IAmStartedByMessages<Message>
{
    public async Task Handle(Message message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
    }
}

public class MyData : ContainSagaData {}";

            await VerifyFix(test, fixTest, allowNewCompilerDiagnostics: true);
        }

        [Test]
        public async Task SagaTimeout()
        {
            string test =
@"using NServiceBus;
using System.Threading.Tasks;

public class MySaga : Saga<MyData>, IHandleTimeouts<Message>
{
}

public class MyData : ContainSagaData {}";

            string fixTest =
@"using NServiceBus;
using System.Threading.Tasks;

public class MySaga : Saga<MyData>, IHandleTimeouts<Message>
{
    public async Task Timeout(Message message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
    }
}

public class MyData : ContainSagaData {}";

            await VerifyFix(test, fixTest, allowNewCompilerDiagnostics: true);
        }

        protected override DiagnosticAnalyzer GetAnalyzer() => new MustImplementIHandleMessagesAnalyzer();

        protected override CodeFixProvider GetCodeFixProvider() => new ImplementIHandleMessagesFixer();
    }
}
