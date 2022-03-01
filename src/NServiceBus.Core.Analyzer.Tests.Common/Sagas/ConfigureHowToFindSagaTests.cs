namespace NServiceBus.Core.Analyzer.Tests.Sagas
{
    using System.Threading.Tasks;
    using Helpers;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    [TestFixture]
    public class ConfigureHowToFindSagaTests : AnalyzerTestFixture<SagaAnalyzer>
    {
        [Test]
        public Task NewMappingInMethodBlock()
        {
            var code = @"
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId)
            .ToMessage<Msg1>(msg => msg.CorrId)
            .ToMessage<Msg2>(msg => msg.CorrId);
    }";

            return RunTest(code, null);
        }

        [Test]
        public Task NewMappingInArrowFunction()
        {
            var code = @"
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper) =>
        mapper.MapSaga(saga => saga.CorrId)
            .ToMessage<Msg1>(msg => msg.CorrId)
            .ToMessage<Msg2>(msg => msg.CorrId);";

            return RunTest(code, null);
        }

        [Test]
        public Task OldMapping()
        {
            var code = @"
    protected override void [|ConfigureHowToFindSaga|](SagaPropertyMapper<MyData> mapper)
    {
        mapper.ConfigureMapping<Msg1>(msg => msg.CorrId).ToSaga(saga => saga.CorrId);
        mapper.ConfigureMapping<Msg2>(msg => msg.CorrId).ToSaga(saga => saga.CorrId);
    }";

            return RunTest(code, SagaDiagnostics.SagaMappingExpressionCanBeSimplifiedId);
        }

        [Test]
        public Task OldMappingWithMultipleCorrelationIds()
        {
            var code = @"
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.ConfigureMapping<Msg1>(msg => msg.CorrId).ToSaga(saga => saga.CorrId);
        mapper.ConfigureMapping<Msg2>(msg => msg.CorrId).ToSaga([|saga => saga.OtherId|]);
    }";

            return RunTest(code, SagaDiagnostics.MultipleCorrelationIdValuesId);
        }

        [Test]
        public Task NonMappingExpressionInMethod()
        {
            var code = @"
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        [|var i = 3 + 4;|]

        mapper.MapSaga(saga => saga.CorrId)
            .ToMessage<Msg1>(msg => msg.CorrId)
            .ToMessage<Msg2>(msg => msg.CorrId);

        [|OtherMethod(i);|]
    }
    void OtherMethod(int i) {}";

            return RunTest(code, SagaDiagnostics.NonMappingExpressionUsedInConfigureHowToFindSagaId);
        }

        protected virtual Task RunTest(string configureHowToFindSagaMethod, string diagnosticId)
        {
            var source =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, IAmStartedByMessages<Msg2>
{
" + configureHowToFindSagaMethod + @"
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
    public Task Handle(Msg2 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
    public string OtherId { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}
public class Msg2 : ICommand
{
    public string CorrId { get; set; }
}";

            return Assert(diagnosticId, source);
        }
    }

    public class ConfigureHowToFindSagaTestsCSharp8 : ConfigureHowToFindSagaTests
    {
        protected override LanguageVersion AnalyzerLanguageVersion => LanguageVersion.CSharp8;

        protected override Task RunTest(string configureHowToFindSagaMethod, string diagnosticId)
        {
            var nullableTypesSource =
@"
#nullable enable
using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, IAmStartedByMessages<Msg2>
{
" + configureHowToFindSagaMethod + @"
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
    public Task Handle(Msg2 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
public class MyData : ContainSagaData
{
    public string? CorrId { get; set; }
    public string? OtherId { get; set; }
}
public class Msg1 : ICommand
{
    public string? CorrId { get; set; }
}
public class Msg2 : ICommand
{
    public string? CorrId { get; set; }
}
#nullable restore";

            return Assert(diagnosticId, nullableTypesSource);
        }
    }

    public class ConfigureHowToFindSagaTestsCSharp9 : ConfigureHowToFindSagaTestsCSharp8
    {
        protected override LanguageVersion AnalyzerLanguageVersion => LanguageVersion.CSharp9;
    }

#if ROSLYN4
    public class ConfigureHowToFindSagaTestsCSharp10 : ConfigureHowToFindSagaTestsCSharp9
    {
        protected override LanguageVersion AnalyzerLanguageVersion => LanguageVersion.CSharp10;
    }
#endif
}
