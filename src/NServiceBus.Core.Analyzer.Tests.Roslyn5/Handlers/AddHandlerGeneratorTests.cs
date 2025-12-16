namespace NServiceBus.Core.Analyzer.Tests.Handlers;

using Analyzer.Handlers;
using Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

public class AddHandlerGeneratorTests
{
    [Test]
    public void BasicHandlers()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddFunny_name_for_kicksHandlers();
                             cfg.AddBarHandlers();
                         }
                     }

                     [HandlerAttribute("Funny name for kicks")]
                     public class Handles1 : IHandleMessages<Cmd1>
                     {
                         public Task Handle(Cmd1 cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     [HandlerAttribute("Bar")]
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

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerGenerator>()
            .WithSource(source, "test.cs")
            .WithGeneratorStages("HandlerSpec", "HandlerSpecs")
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void InterfaceLessHandlers()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddFooHandlers();
                         }
                     }

                     [HandlerAttribute("Foo")]
                     public class InterfaceLess
                     {
                         public Task Handle(MyCommand message, IMessageHandlerContext context, IMyService service) => Task.CompletedTask;
                     }

                     [HandlerAttribute("Foo")]
                     public class InterfaceLessNoDependencies
                     {
                         public Task Handle(MyEvent message, IMessageHandlerContext context) => Task.CompletedTask;
                     }
                     
                     [HandlerAttribute("Foo")]
                     public class InterfaceLessStatic
                     {
                         public static Task Handle(MyCommand message, IMessageHandlerContext context) => Task.CompletedTask;
                     }
                     
                     [HandlerAttribute("Foo")]
                     public class InterfaceLessStaticWithDependencies
                     {
                         public static Task Handle(MyCommand message, IMessageHandlerContext context, IMyService service) => Task.CompletedTask;
                     }

                     public class MyCommand : ICommand { }
                     public class MyEvent : IEvent { }
                     public interface IMyService { }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<AddHandlerGenerator>()
            .AddReference(MetadataReference.CreateFromFile(typeof(ActivatorUtilities).Assembly.Location))
            .WithSource(source, "test.cs")
            .WithGeneratorStages("HandlerSpec", "HandlerSpecs")
            .Approve()
            .AssertRunsAreEqual();
    }
}