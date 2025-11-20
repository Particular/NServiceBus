namespace NServiceBus.Core.Analyzer.Tests.Handlers;

using System.Threading.Tasks;
using Analyzer.Handlers;
using Helpers;
using NUnit.Framework;

[TestFixture]
public class HandlerInjectsMessageSessionTests : AnalyzerTestFixture<HandlerInjectsMessageSessionAnalyzer>
{
    [TestCase("IMessageSession")]
    [TestCase("IEndpointInstance")]
    public Task MultiTest(string injectedType)
    {
        var source = $$"""
                     using System;
                     using System.Threading.Tasks;
                     using NServiceBus;
                     public class MyHandler : IHandleMessages<MyMessage>
                     {
                        public MyHandler(string isFine, [|{{injectedType}}|] notGood) { }
                        
                        public string IsFine { get; set; }
                        public [|{{injectedType}}|] NotGood { get; set; }
                     
                        public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
                     }
                     public class MyMessage : ICommand { }
                     """;

        return Assert(DiagnosticIds.HandlerInjectsMessageSession, source);
    }

    [TestCase("IMessageSession")]
    [TestCase("IEndpointInstance")]
    public Task PrimaryConstructor(string injectedType)
    {
        var source = $$"""
                       using System;
                       using System.Threading.Tasks;
                       using NServiceBus;
                       public class MyHandler(string isFine, [|{{injectedType}}|] notGood) : IHandleMessages<MyMessage>
                       {
                          public string IsFine { get; set; } = isFine;
                          public [|{{injectedType}}|] NotGood { get; } = notGood;
                          
                          public [|{{injectedType}}|] AlsoNotGood
                          {
                            get { return null; }
                            set { }
                          }

                          public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
                       }
                       public class MyMessage : ICommand { }
                       """;

        return Assert(DiagnosticIds.HandlerInjectsMessageSession, source);
    }
}