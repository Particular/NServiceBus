namespace NServiceBus.Core.Analyzer.Tests.Sagas
{
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Helpers;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    [TestFixture]
    public class RewriteConfigureHowToFindSagaFixerTests : CodeFixTestFixture<SagaAnalyzer, RewriteConfigureHowToFindSagaFixer>
    {
        [Test]
        public Task Simple()
        {
            var original =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, IAmStartedByMessages<Msg2>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.ConfigureMapping<Msg1>(msg => msg.CorrId).ToSaga(saga => saga.CorrId);
        mapper.ConfigureMapping<Msg2>(msg => msg.CorrId).ToSaga(saga => saga.CorrId);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
    public Task Handle(Msg2 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}
public class Msg2 : ICommand
{
    public string CorrId { get; set; }
}";

            var expected =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, IAmStartedByMessages<Msg2>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId)
            .ToMessage<Msg1>(msg => msg.CorrId)
            .ToMessage<Msg2>(msg => msg.CorrId);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
    public Task Handle(Msg2 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}
public class Msg2 : ICommand
{
    public string CorrId { get; set; }
}";

            return Assert(original, expected);
        }

        [TestCase(null)]
        [TestCase("\r\n")]
        [TestCase("\n")]
        public Task SimpleLineEndingTestWithExtraNewLines(string lineEnding)
        {
            var original =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, IAmStartedByMessages<Msg2>
{ // Extra newlines are on purpose


{TAB}{SPACE}


    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.ConfigureMapping<Msg1>(msg => msg.CorrId).ToSaga(saga => saga.CorrId);
        mapper.ConfigureMapping<Msg2>(msg => msg.CorrId).ToSaga(saga => saga.CorrId);
    }



    public Task Handle(Msg1 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
    public Task Handle(Msg2 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}
public class Msg2 : ICommand
{
    public string CorrId { get; set; }
}";

            var expected =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, IAmStartedByMessages<Msg2>
{ // Extra newlines are on purpose


{TAB}{SPACE}


    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId)
            .ToMessage<Msg1>(msg => msg.CorrId)
            .ToMessage<Msg2>(msg => msg.CorrId);
    }



    public Task Handle(Msg1 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
    public Task Handle(Msg2 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}
public class Msg2 : ICommand
{
    public string CorrId { get; set; }
}";

            original = original.Replace("{TAB}", "\t").Replace("{SPACE}", " ");
            expected = expected.Replace("{TAB}", "\t").Replace("{SPACE}", " ");

            if (lineEnding != null)
            {
                original = Regex.Replace(original, "\r?\n", lineEnding);
                expected = Regex.Replace(expected, "\r?\n", lineEnding);
            }

            return Assert(original, expected);
        }

        [Test]
        public Task IndentedInNamespace()
        {
            var original =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
namespace SomeNamespace
{
    public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, IAmStartedByMessages<Msg2>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
        {
            mapper.ConfigureMapping<Msg1>(msg => msg.CorrId).ToSaga(saga => saga.CorrId);
            mapper.ConfigureMapping<Msg2>(msg => msg.CorrId).ToSaga(saga => saga.CorrId);
        }
        public Task Handle(Msg1 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
        public Task Handle(Msg2 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
    }
    public class MyData : ContainSagaData
    {
        public string CorrId { get; set; }
    }
    public class Msg1 : ICommand
    {
        public string CorrId { get; set; }
    }
    public class Msg2 : ICommand
    {
        public string CorrId { get; set; }
    }
}";

            var expected =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
namespace SomeNamespace
{
    public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, IAmStartedByMessages<Msg2>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
        {
            mapper.MapSaga(saga => saga.CorrId)
                .ToMessage<Msg1>(msg => msg.CorrId)
                .ToMessage<Msg2>(msg => msg.CorrId);
        }
        public Task Handle(Msg1 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
        public Task Handle(Msg2 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
    }
    public class MyData : ContainSagaData
    {
        public string CorrId { get; set; }
    }
    public class Msg1 : ICommand
    {
        public string CorrId { get; set; }
    }
    public class Msg2 : ICommand
    {
        public string CorrId { get; set; }
    }
}";

            return Assert(original, expected);
        }

        [Test]
        public Task KitchenSinkMappings()
        {
            var original =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
namespace SomeNamespace
{
    public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, IAmStartedByMessages<Msg2>, IAmStartedByMessages<Msg3>, IAmStartedByMessages<Msg4>, IAmStartedByMessages<Msg5>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
        {
            mapper.ConfigureMapping<Msg1>(msg => msg.Foo).ToSaga(saga => saga.CorrId);
            mapper.ConfigureMapping<Msg2>(msg => msg.Foo + "" "" + msg.Bar).ToSaga(saga => saga.CorrId);
            mapper.ConfigureMapping<Msg3>(msg => $""{msg.Foo}/{msg.Bar}/{msg.Baz}"").ToSaga(saga => saga.CorrId);
            mapper.ConfigureHeaderMapping<Msg4>(""speed"").ToSaga(saga => saga.CorrId);
            mapper.ConfigureHeaderMapping<Msg5>(""racer"").ToSaga(saga => saga.CorrId);
        }
        public Task Handle(Msg1 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
        public Task Handle(Msg2 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
        public Task Handle(Msg3 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
        public Task Handle(Msg4 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
        public Task Handle(Msg5 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
    }
    public class MyData : ContainSagaData
    {
        public string CorrId { get; set; }
    }
    public class Msg1 : ICommand
    {
        public string Foo { get; set; }
    }
    public class Msg2 : ICommand
    {
        public string Foo { get; set; }
        public string Bar { get; set; }
    }
    public class Msg3 : ICommand
    {
        public string Foo { get; set; }
        public string Bar { get; set; }
        public string Baz { get; set; }
    }
    public class Msg4 : ICommand {}
    public class Msg5 : ICommand {}
}";

            var expected =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
namespace SomeNamespace
{
    public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, IAmStartedByMessages<Msg2>, IAmStartedByMessages<Msg3>, IAmStartedByMessages<Msg4>, IAmStartedByMessages<Msg5>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
        {
            mapper.MapSaga(saga => saga.CorrId)
                .ToMessage<Msg1>(msg => msg.Foo)
                .ToMessage<Msg2>(msg => msg.Foo + "" "" + msg.Bar)
                .ToMessage<Msg3>(msg => $""{msg.Foo}/{msg.Bar}/{msg.Baz}"")
                .ToMessageHeader<Msg4>(""speed"")
                .ToMessageHeader<Msg5>(""racer"");
        }
        public Task Handle(Msg1 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
        public Task Handle(Msg2 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
        public Task Handle(Msg3 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
        public Task Handle(Msg4 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
        public Task Handle(Msg5 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
    }
    public class MyData : ContainSagaData
    {
        public string CorrId { get; set; }
    }
    public class Msg1 : ICommand
    {
        public string Foo { get; set; }
    }
    public class Msg2 : ICommand
    {
        public string Foo { get; set; }
        public string Bar { get; set; }
    }
    public class Msg3 : ICommand
    {
        public string Foo { get; set; }
        public string Bar { get; set; }
        public string Baz { get; set; }
    }
    public class Msg4 : ICommand {}
    public class Msg5 : ICommand {}
}";

            return Assert(original, expected);
        }

        [Test]
        public Task AddMissingMessageMappingFallbackValue()
        {
            var original =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, IAmStartedByMessages<Msg2>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId)
            .ToMessage<Msg1>(msg => msg.CorrId);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
    public Task Handle(Msg2 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}
public class Msg2 : ICommand
{
}";

            var expected =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, IAmStartedByMessages<Msg2>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId)
            .ToMessage<Msg1>(msg => msg.CorrId)
            .ToMessage<Msg2>(msg => msg.MessagePropertyWithCorrelationValue);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
    public Task Handle(Msg2 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}
public class Msg2 : ICommand
{
}";

            return Assert(original, expected, fixMustCompile: false);
        }

        [Test]
        public Task AddMissingMessageMappingWhenPropertyMatchesCorrelation()
        {
            var original =
    @"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, IAmStartedByMessages<Msg2>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId)
            .ToMessage<Msg1>(msg => msg.CorrId);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
    public Task Handle(Msg2 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}
public class Msg2 : ICommand
{
    public string CorrId { get; set; }
}";

            var expected =
    @"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, IAmStartedByMessages<Msg2>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId)
            .ToMessage<Msg1>(msg => msg.CorrId)
            .ToMessage<Msg2>(msg => msg.CorrId);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
    public Task Handle(Msg2 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}
public class Msg2 : ICommand
{
    public string CorrId { get; set; }
}";

            return Assert(original, expected);
        }

        /// <summary>
        /// NOTE: Don't need to test when ConfigureHowToFindSaga method is missing completely, because
        /// that's a compile error: the abstract base class method is not implemented
        /// </summary>
        [Test]
        public Task AddMissingMessageMappingWhenMappingMethodIsEmpty()
        {
            var original =
    @"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}";

            var expected =
    @"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrelationPropertyName)
            .ToMessage<Msg1>(msg => msg.MessagePropertyWithCorrelationValue);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}";

            return Assert(original, expected, fixMustCompile: false);
        }

        [Test]
        public Task RewriteSingleOldMappingWithInfoDiagnostic()
        {
            var original =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.ConfigureMapping<Msg1>(msg => msg.CorrId).ToSaga(saga => saga.CorrId);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
    public string OtherId { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}";

            var expected =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId)
            .ToMessage<Msg1>(msg => msg.CorrId);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) { throw new NotImplementedException(); }
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
    public string OtherId { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}";

            return Assert(original, expected);
        }
    }

    public class RewriteConfigureHowToFindSagaFixerTestsCSharp8 : RewriteConfigureHowToFindSagaFixerTests
    {
        protected override LanguageVersion AnalyzerLanguageVersion => LanguageVersion.CSharp8;
    }

    public class RewriteConfigureHowToFindSagaFixerTestsCSharp9 : RewriteConfigureHowToFindSagaFixerTestsCSharp8
    {
        protected override LanguageVersion AnalyzerLanguageVersion => LanguageVersion.CSharp9;
    }

#if ROSLYN4
    public class RewriteConfigureHowToFindSagaFixerTestsCSharp10 : RewriteConfigureHowToFindSagaFixerTestsCSharp9
    {
        protected override LanguageVersion AnalyzerLanguageVersion => LanguageVersion.CSharp10;
    }
#endif
}