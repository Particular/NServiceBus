namespace NServiceBus.Core.Analyzer.Tests.Handlers;

using Analyzer.Handlers;
using Analyzer.Sagas;
using Helpers;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

public class AddHandlerInterceptorTests
{
    [Test]
    public void BasicHandlers()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         [NServiceBusRegistrations]
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddHandler<Handles1>();
                             cfg.AddHandler<Handles3>();
                             // Duplicate call, methods should be deduped with 2 InterceptsLocation attributes
                             cfg.AddHandler<Handles3>();
                         }
                     }

                     public class Handles1 : IHandleMessages<Cmd1>
                     {
                         public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class Handles3 : IHandleMessages<Cmd1>, IHandleMessages<Cmd2>, IHandleMessages<Evt1>
                     {
                         public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(Cmd2 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(Evt1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }
                     public class Cmd1 : CmdBase { }
                     public class Cmd2 : ICommand { }
                     public class Evt1 : IEvent { }
                     public class CmdBase : ICommand { }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerInterceptor>()
            .WithSource(source, "test.cs")
            .WithGeneratorStages("HandlerSpec", "HandlerSpecs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void AttributeOnClass()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     [NServiceBusRegistrations]
                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddHandler<Handles1>();
                             cfg.AddHandler<Handles3>();
                             // Duplicate call, methods should be deduped with 2 InterceptsLocation attributes
                             cfg.AddHandler<Handles3>();
                         }
                     }

                     public class Handles1 : IHandleMessages<Cmd1>
                     {
                         public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class Handles3 : IHandleMessages<Cmd1>, IHandleMessages<Cmd2>, IHandleMessages<Evt1>
                     {
                         public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(Cmd2 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(Evt1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }
                     public class Cmd1 : ICommand { }
                     public class Cmd2 : ICommand { }
                     public class Evt1 : IEvent { }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerInterceptor>()
            .WithSource(source, "test.cs")
            .WithGeneratorStages("HandlerSpec", "HandlerSpecs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void NoAttributeNoOutput()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddHandler<Handles1>();
                             cfg.AddHandler<Handles3>();
                             // Duplicate call, methods should be deduped with 2 InterceptsLocation attributes
                             cfg.AddHandler<Handles3>();
                         }
                     }

                     public class Handles1 : IHandleMessages<Cmd1>
                     {
                         public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class Handles3 : IHandleMessages<Cmd1>, IHandleMessages<Cmd2>, IHandleMessages<Evt1>
                     {
                         public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(Cmd2 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(Evt1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }
                     public class Cmd1 : ICommand { }
                     public class Cmd2 : ICommand { }
                     public class Evt1 : IEvent { }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerInterceptor>()
            .WithSource(source, "test.cs")
            .WithGeneratorStages("HandlerSpec", "HandlerSpecs")
            .ShouldNotGenerateCode()
            .AssertRunsAreEqual();
    }

    [Test]
    public void SagaWithInappropriateDoubleMessageMapping()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         [NServiceBusRegistrations]
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddHandler<OrderPolicy>();
                         }
                     }

                     public class OrderPolicy : Saga<OrderPolicyData>,
                         IAmStartedByMessages<OrderPlaced>,
                         IAmStartedByMessages<OrderBilled>,
                         IHandleTimeouts<OrderPlaced> // Should not also use a message as timeout state in real life!
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderPolicyData> mapper)
                         {
                             mapper.MapSaga(saga => saga.OrderId)
                                 .ToMessage<OrderPlaced>(msg => msg.OrderId)
                                 .ToMessage<OrderBilled>(msg => msg.OrderId);
                         }
                         
                         public Task Handle(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(OrderBilled evt, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Timeout(OrderPlaced evt, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class OrderPolicyData : ContainSagaData
                     {
                         public string OrderId { get; set; }
                     }
                     public class OrderPlaced : IEvent
                     {
                         public string OrderId { get; set; }
                     }
                     public class OrderBilled : IEvent
                     {
                         public string OrderId { get; set; }
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerInterceptor>()
            .WithSource(source, "test.cs")
            .WithGeneratorStages("HandlerSpec", "HandlerSpecs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void TopLevelStatements()
    {
        var program = """
                      using NServiceBus;
                      
                      var cfg = new EndpointConfiguration("TestEndpoint");
                      cfg.AddHandler<Handles1>();
                      cfg.AddHandler<Handles3>();
                      // Duplicate call, methods should be deduped with 2 InterceptsLocation attributes
                      cfg.AddHandler<Handles3>();
                      
                      await Endpoint.Start(cfg);
                      """;

        var partialProgram = """
                             [NServiceBus.NServiceBusRegistrations]
                             partial class Program { }
                             """;

        var handlers = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Handles1 : IHandleMessages<Cmd1>
                     {
                         public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class Handles3 : IHandleMessages<Cmd1>, IHandleMessages<Cmd2>, IHandleMessages<Evt1>
                     {
                         public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(Cmd2 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(Evt1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }
                     public class Cmd1 : CmdBase { }
                     public class Cmd2 : ICommand { }
                     public class Evt1 : IEvent { }
                     public class CmdBase : ICommand { }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerInterceptor>()
            .WithSource(program, "Program.cs")
            .WithSource(partialProgram, "ProgramPartial.cs")
            .WithSource(handlers, "Handlers.cs")
            .BuildAs(OutputKind.ConsoleApplication)
            .WithGeneratorStages("HandlerSpec", "HandlerSpecs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void DecoratedPartialMethod()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public partial class Test
                     {
                         public partial void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddHandler<Handles1>();
                             cfg.AddHandler<Handles3>();
                             // Duplicate call, methods should be deduped with 2 InterceptsLocation attributes
                             cfg.AddHandler<Handles3>();
                         }
                     }

                     public class Handles1 : IHandleMessages<Cmd1>
                     {
                         public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class Handles3 : IHandleMessages<Cmd1>, IHandleMessages<Cmd2>, IHandleMessages<Evt1>
                     {
                         public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(Cmd2 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(Evt1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }
                     public class Cmd1 : CmdBase { }
                     public class Cmd2 : ICommand { }
                     public class Evt1 : IEvent { }
                     public class CmdBase : ICommand { }
                     """;

        var partial = """
                      public partial class Test
                      {
                          [NServiceBus.NServiceBusRegistrations]
                          public partial void Configure(NServiceBus.EndpointConfiguration cfg);
                      }
                      """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerInterceptor>()
            .WithSource(source, "test.cs")
            .WithSource(partial, "partial.cs")
            .WithGeneratorStages("HandlerSpec", "HandlerSpecs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void DecoratedPartialMethodOtherSide()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public partial class Test
                     {
                         [NServiceBus.NServiceBusRegistrations]
                         public partial void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddHandler<Handles1>();
                             cfg.AddHandler<Handles3>();
                             // Duplicate call, methods should be deduped with 2 InterceptsLocation attributes
                             cfg.AddHandler<Handles3>();
                         }
                     }

                     public class Handles1 : IHandleMessages<Cmd1>
                     {
                         public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class Handles3 : IHandleMessages<Cmd1>, IHandleMessages<Cmd2>, IHandleMessages<Evt1>
                     {
                         public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(Cmd2 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(Evt1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }
                     public class Cmd1 : CmdBase { }
                     public class Cmd2 : ICommand { }
                     public class Evt1 : IEvent { }
                     public class CmdBase : ICommand { }
                     """;

        var partial = """
                      public partial class Test
                      {
                          public partial void Configure(NServiceBus.EndpointConfiguration cfg);
                      }
                      """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerInterceptor>()
            .WithSource(source, "test.cs")
            .WithSource(partial, "partial.cs")
            .WithGeneratorStages("HandlerSpec", "HandlerSpecs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void DecoratedPartialClass()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public partial class Test
                     {
                         public partial void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddHandler<Handles1>();
                             cfg.AddHandler<Handles3>();
                             // Duplicate call, methods should be deduped with 2 InterceptsLocation attributes
                             cfg.AddHandler<Handles3>();
                         }
                     }

                     public class Handles1 : IHandleMessages<Cmd1>
                     {
                         public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class Handles3 : IHandleMessages<Cmd1>, IHandleMessages<Cmd2>, IHandleMessages<Evt1>
                     {
                         public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(Cmd2 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(Evt1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }
                     public class Cmd1 : CmdBase { }
                     public class Cmd2 : ICommand { }
                     public class Evt1 : IEvent { }
                     public class CmdBase : ICommand { }
                     """;

        var partial = """
                      [NServiceBus.NServiceBusRegistrations]
                      public partial class Test
                      {
                          public partial void Configure(NServiceBus.EndpointConfiguration cfg);
                      }
                      """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerInterceptor>()
            .WithSource(source, "test.cs")
            .WithSource(partial, "partial.cs")
            .WithGeneratorStages("HandlerSpec", "HandlerSpecs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void DecoratedPartialClassOtherSide()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     [NServiceBus.NServiceBusRegistrations]
                     public partial class Test
                     {
                         public partial void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddHandler<Handles1>();
                             cfg.AddHandler<Handles3>();
                             // Duplicate call, methods should be deduped with 2 InterceptsLocation attributes
                             cfg.AddHandler<Handles3>();
                         }
                     }

                     public class Handles1 : IHandleMessages<Cmd1>
                     {
                         public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class Handles3 : IHandleMessages<Cmd1>, IHandleMessages<Cmd2>, IHandleMessages<Evt1>
                     {
                         public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(Cmd2 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                         public Task Handle(Evt1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }
                     public class Cmd1 : CmdBase { }
                     public class Cmd2 : ICommand { }
                     public class Evt1 : IEvent { }
                     public class CmdBase : ICommand { }
                     """;

        var partial = """
                      public partial class Test
                      {
                          public partial void Configure(NServiceBus.EndpointConfiguration cfg);
                      }
                      """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerInterceptor>()
            .WithSource(source, "test.cs")
            .WithSource(partial, "partial.cs")
            .WithGeneratorStages("HandlerSpec", "HandlerSpecs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void AddHandlerUsedOnSaga()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     [NServiceBus.NServiceBusRegistrations]
                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             // Can't call AddHandler on a Saga
                             cfg.AddHandler<MySaga>();
                             
                             // Don't need to check AddSaga<MyHandler>(), generic constraint prevents this
                         }
                     }

                     public class MyHandler : IHandleMessages<Cmd>
                     {
                         public Task Handle(Cmd cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }
                     
                     public class MySaga : Saga<MyData>, IAmStartedByMessages<Cmd>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
                         {
                             mapper.MapSaga(s => s.Corr)
                                .ToMessage<Cmd>(c => c.Corr);
                         }
                         
                         public Task Handle(Cmd cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }
                     
                     public class MyData : ContainSagaData
                     {
                         public string Corr { get; set; }
                     }
                     public class Cmd : ICommand
                     {
                         public string Corr { get; set; }
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerInterceptor>()
            .WithIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(source, "test.cs")
            .WithGeneratorStages("HandlerSpec", "HandlerSpecs")
            .ToConsole()
            .AssertRunsAreEqual();
    }
}