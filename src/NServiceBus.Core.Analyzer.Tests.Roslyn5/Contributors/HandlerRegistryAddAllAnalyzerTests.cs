namespace NServiceBus.Core.Analyzer.Tests.Contributors;

using System.Threading.Tasks;
using NUnit.Framework;
using Particular.AnalyzerTesting;

public class HandlerRegistryAddAllAnalyzerTests : AnalyzerTestFixture<HandlerRegistryAddAllAnalyzer>
{
    [Test]
    public Task DoesNotReportDirectCalls()
    {
        var source = """
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.AddAll();
                             cfg.Handlers.AddAllHandlers();
                             cfg.Handlers.AddAllSagas();
                         }
                     }
                     """;

        return Assert(source);
    }

    [Test]
    public Task ReportsMethodGroupConversions()
    {
        var source = """
                     using System;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             Action all = [|cfg.Handlers.AddAll|];
                             var handlers = new Action([|cfg.Handlers.AddAllHandlers|]);
                             Action sagas = [|cfg.Handlers.AddAllSagas|];
                         }
                     }
                     """;

        return Assert(source, DiagnosticIds.HandlerRegistryAddAllMustBeCalledDirectly);
    }

    [Test]
    public Task DoesNotReportOtherMethodGroups()
    {
        var source = """
                     using System;

                     public class Test
                     {
                         public void Configure(Other other)
                         {
                             Action all = other.AddAll;
                         }
                     }

                     public class Other
                     {
                         public void AddAll() { }
                     }
                     """;

        return Assert(source);
    }
}
