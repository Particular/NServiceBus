namespace NServiceBus.Core.Analyzer.Tests.Handlers;

using HandlerRegistration;
using Helpers;
using NUnit.Framework;

public class RegisterHandlerInterceptorTests
{
    [Test]
    public void BasicHandlers()
    {
        var source = $$"""
                     using System.Threading.Tasks;
                     using NServiceBus;
                     
                     public class Test
                     {
                        public void Configure(EndpointConfiguration cfg)
                        {
                            cfg.RegisterHandler<Handles1>();
                            cfg.RegisterHandler<Handles3>();
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

        SourceGeneratorTest.ForIncrementalGenerator<RegisterHandlerInterceptor>()
            .WithSource(source, "test.cs")
            .WithGeneratorStages("InterceptCandidates", "Two")
            .Approve()
            .ToConsole()
            .AssertRunsAreEqual();
    }
}