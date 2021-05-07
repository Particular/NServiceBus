namespace NServiceBus.Core.Analyzer.Tests
{
    using System.Threading.Tasks;
    using Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class ForwardCancellationTokenTests : AnalyzerTestFixture<ForwardCancellationTokenAnalyzer>
    {
        [TestCase("IMessageHandlerContext context")]
        [TestCase("int a, string b, IMessageHandlerContext context")]
        [TestCase("IMessageHandlerContext context, int a, string b")]
        [TestCase("int a, string b, IMessageHandlerContext context, float c, double d")]
        [TestCase("IMessageHandlerContext context, string s = null, int i = 0")]
        [TestCase("[Silly] IMessageHandlerContext context")]
        [TestCase("IMessageHandlerContext mc")]
        [TestCase("ICancellableContext context")]
        [TestCase("IPipelineContext context")]
        [TestCase("IMessageProcessingContext context")]
        [TestCase("IBehaviorContext context")]
        [TestCase("IIncomingLogicalMessageContext context")]
        [TestCase("IOutgoingLogicalMessageContext context")]
        [TestCase("IIncomingPhysicalMessageContext context")]
        [TestCase("IOutgoingPhysicalMessageContext context")]
        [TestCase("MutateIncomingMessageContext context")]
        [TestCase("MutateOutgoingMessageContext context")]
        [TestCase("MutateIncomingTransportMessageContext context")]
        [TestCase("MutateOutgoingTransportMessageContext context")]
        public Task NormalUsage(string arguments) => Assert(
            ForwardCancellationTokenAnalyzer.DiagnosticId,
@"using NServiceBus;
using NServiceBus.MessageMutator;
using NServiceBus.Pipeline;
using System.Threading;
using System.Threading.Tasks;
public class Thing
{
    public Task OtherClassMethod(CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
}
public static class ThingExtensions
{
    public static Task ExtensionMethod(this Thing thing, bool value, CancellationToken token = default(CancellationToken))
    {
        return Task.CompletedTask;
    }
}
public class DerivedClass : BaseClass
{
    public Task TokenMethodOnBase() { return base.TokenMethodOnBase(CancellationToken.None); }
    public Task TokenMethodOnDerived(CancellationToken token) { return Task.CompletedTask; }
}
public class BaseClass
{
    public Task TokenMethodOnBase(CancellationToken token) { return Task.CompletedTask; }
    public Task TokenMethodOnDerived() { return Task.CompletedTask; }
}
[System.AttributeUsage(System.AttributeTargets.All)]
public class SillyAttribute : System.Attribute { }
public class Foo
{
    static Task StaticMethod(CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
    Task InstanceMethod(CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }

    static Task LotsOfParameters(bool p1, bool p2, bool p3, bool p4, bool p5, CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }

    static Task LotsOfOverloadsAllTakeToken(bool p1, CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
    static Task LotsOfOverloadsAllTakeToken(bool p1, bool p2, CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
    static Task LotsOfOverloadsAllTakeToken(bool p1, bool p2, bool p3, CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
    static Task LotsOfOverloadsAllTakeToken(bool p1, bool p2, bool p3, bool p4, CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
    static Task LotsOfOverloadsAllTakeToken(bool p1, bool p2, bool p3, bool p4, bool p5, CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }

    static Task LotsOfOverloadsOneTakesToken(bool p1) { return Task.CompletedTask; }
    static Task LotsOfOverloadsOneTakesToken(bool p1, bool p2) { return Task.CompletedTask; }
    static Task LotsOfOverloadsOneTakesToken(bool p1, bool p2, bool p3) { return Task.CompletedTask; }
    static Task LotsOfOverloadsOneTakesToken(bool p1, bool p2, bool p3, bool p4) { return Task.CompletedTask; }
    static Task LotsOfOverloadsOneTakesToken(bool p1, bool p2, bool p3, bool p4, bool p5, CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
    
    static Task Overloads(bool p1) { return Task.CompletedTask; }
    static Task Overloads(bool p1, CancellationToken cancellationToken) { return Task.CompletedTask; }

    public async Task Method(" + arguments + @")
    {
        var thing = new Thing();
        var derived = new DerivedClass();

        await [|derived.TokenMethodOnBase()|];
        await [|derived.TokenMethodOnDerived()|];
    }
}");

        [Test]
        public Task DelegateBody() => Assert(
            ForwardCancellationTokenAnalyzer.DiagnosticId,
@"using System;
using System.Threading;
using NServiceBus;

public class MyClass
{
    void MyMethod(CancellationToken cancellationToken = default(CancellationToken)) { }

    void Foo(ICancellableContext context)
    {
        Action<ICancellableContext> myAction1 = delegate(ICancellableContext _) { [|MyMethod()|]; };
        Action<ICancellableContext> myAction2 = _ => [|MyMethod()|];
        Action<ICancellableContext> myAction3 = (_) => { [|MyMethod()|]; };

        // test that the context param of Foo isn't considered
        // CA2016 doesn't raise a diagnostic for the equivalent case with CancellationToken
        Action myAction4 = () => { MyMethod(); };
    }
}");

        [Test]
        public Task NoBaseType() => Assert(
            ForwardCancellationTokenAnalyzer.DiagnosticId,
@"using NServiceBus;
using System;
using System.Threading;
using System.Threading.Tasks;
public class Foo
{
    public async Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        await [|TestMethod()|];
        await [|Foo.TestMethod()|];
        await [|TestMethod()|];
        await [|Foo.TestMethod()|];
    }

    public async Task Invoke(IMadeUpContext context, Func<IMadeUpContext, Task> next)
    {
        await TestMethod();
        await Foo.TestMethod();
        await TestMethod();
        await Foo.TestMethod();
    }

    static Task TestMethod(CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
}
public class TestMessage : ICommand {}
public interface IMadeUpContext {}");

        [Test]
        public Task NoDiagnosticWhenNotNServiceBus() => Assert(
@"using System;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public async Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        await TestMethod();
        await Foo.TestMethod();
        await TestMethod();
        await Foo.TestMethod();
    }

    static Task TestMethod(CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
}
public class Bar : Behavior<IMadeUpContext>
{
    public async override Task Invoke(IMadeUpContext context, Func<Task> next)
    {
        await TestMethod();
        await Bar.TestMethod();
        await TestMethod();
        await Bar.TestMethod();
        await next();
    }

    static Task TestMethod(CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
}
public class TestMessage {}
public interface IMessageHandlerContext {}
public interface IMadeUpContext {}
public interface IHandleMessages<T>
{
    Task Handle(T message, IMessageHandlerContext context);
}
public abstract class Behavior<TContext>
{
    public abstract Task Invoke(TContext context, Func<Task> next);
}");

        [Test]
        public Task NoDiagnosticWhenTokenPassedToStaticMethod() => Assert(
@"using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public async Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        await TestMethod(context.CancellationToken);
        await Foo.TestMethod(context.CancellationToken);
        await TestMethod(new CancellationToken(false));
        await Foo.TestMethod(new CancellationToken(false));
    }

    static Task TestMethod(CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
}
public class TestMessage : ICommand {}");

        [Test]
        public Task NoDiagnosticWhenStaticMethodDoesNotSupportToken() => Assert(
@"using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public async Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        await TestMethod(true);
        await Foo.TestMethod(true);
    }

    static Task TestMethod(bool value, string optionalParam = null) { return Task.CompletedTask; }
}
public class TestMessage : ICommand {}");

        [Test]
        public Task NoDiagnosticWhenTokenPassedToExternalStaticMethod() => Assert(
@"using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public async Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        await OtherClass.TestMethod(context.CancellationToken);
        await OtherClass.TestMethod(new CancellationToken(false));
    }
}
public static class OtherClass
{
    public static Task TestMethod(CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
}
public class TestMessage : ICommand {}");

        [Test]
        public Task NoDiagnosticWhenExternalStaticMethodDoesNotSupportToken() => Assert(
@"using NServiceBus;
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
    public static Task TestMethod(bool value) { return Task.CompletedTask; }
}
public class TestMessage : ICommand {}");

        [Test]
        public Task NoDiagnosticWhenTokenPassedToExternalStaticMethodWithStaticUsing() => Assert(
@"using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
using static OtherClass;
public class Foo : IHandleMessages<TestMessage>
{
    public async Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        await TestMethod(context.CancellationToken);
        await TestMethod(new CancellationToken(false));
    }
}
public static class OtherClass
{
    public static Task TestMethod(CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
}
public class TestMessage : ICommand {}");

        [Test]
        public Task NoDiagnosticWhenExternalStaticMethodDoesNotSupportTokenWithStaticUsing() => Assert(
@"using NServiceBus;
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
    public static Task TestMethod(bool value) { return Task.CompletedTask; }
}
public class TestMessage : ICommand {}");

        [Test]
        public Task NoDiagnosticWhenTokenPassedToMemberMethod() => Assert(
@"using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public async Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        await TestMethod(context.CancellationToken);
        await this.TestMethod(context.CancellationToken);
        await TestMethod(new CancellationToken(false));
        await this.TestMethod(new CancellationToken(false));
    }

    Task TestMethod(CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
}
public class TestMessage : ICommand {}");

        [Test]
        public Task NoDiagnosticWhenMemberMethodDoesNotSupportToken() => Assert(
@"using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public async Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        await TestMethod(true);
        await this.TestMethod(true);
    }

    Task TestMethod(bool value, string optionalParam = null) { return Task.CompletedTask; }
}
public class TestMessage : ICommand {}");

        [Test]
        public Task NoDiagnosticWhenMethodCandidateTakesASecondRequiredToken() => Assert(
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
    Task TestMethod(CancellationToken token1, CancellationToken token2) { return Task.CompletedTask; }
}
public class TestMessage : ICommand {}");

        [Test]
        public Task NoDiagnosticIfMethodCandidateDoesntMatchExistingParameters() => Assert(
@"using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public async Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        await TestMethod(true, false);
        await this.TestMethod(false, true);
    }

    Task TestMethod(bool value1, bool value2) { return Task.CompletedTask; }
    Task TestMethod(string value1, string value2, CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
}
public class TestMessage : ICommand {}");

        [Test]
        public Task NoDiagnosticOnNamedRandomOrderParameters() => Assert(
@"using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public async Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        await DoSomething(token: context.CancellationToken, myBool: true, myInt: 42);
    }

    private Task DoSomething(int myInt = 0, bool myBool = false, string myString = null, CancellationToken token = default(CancellationToken))
    {
        return Task.CompletedTask;
    }
}
public class TestMessage : ICommand {}");

        [Test]
        public Task NoDiagnosticOnNamedRandomOrderParametersInExtensionMethod() => Assert(
@"using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public async Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        var bar = new Bar();
        await bar.DoSomething(token: context.CancellationToken, myBool: true, myInt: 42);
    }
}
public class TestMessage : ICommand {}
public class Bar {}
public static class BarExtensions
{
    public static Task DoSomething(this Bar bar, int myInt = 0, bool myBool = false, string myString = null, CancellationToken token = default(CancellationToken))
    {
        return Task.CompletedTask;
    }
}");

        [Test]
        public Task NoDiagnosticOnCrazyWaysToCreateAToken() => Assert(
@"using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
public class Foo : IHandleMessages<TestMessage>
{
    public async Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        await DoSomething(true, new CancellationToken(true));
        await DoSomething(true, new CancellationToken(false));
        await DoSomething(true, CancellationToken.None);

        var tokenSource = new CancellationTokenSource();
        await DoSomething(true, tokenSource.Token);
        await DoSomething(true, MakeToken());
        await DoSomething(true, this.MakeToken());
        await DoSomething(true, MyToken);
        await DoSomething(true, this.MyToken);
        await DoSomething(true, privateToken);
        await DoSomething(true, this.privateToken);
    }

    private Task DoSomething(bool value, CancellationToken token = default(CancellationToken))
    {
        return Task.CompletedTask;
    }

    private CancellationToken MakeToken()
    {
        return new CancellationToken(false);
    }

    public CancellationToken MyToken => CancellationToken.None;

    private CancellationToken privateToken = CancellationToken.None;
}
public class TestMessage : ICommand {}
public class Bar {}");
    }
}
