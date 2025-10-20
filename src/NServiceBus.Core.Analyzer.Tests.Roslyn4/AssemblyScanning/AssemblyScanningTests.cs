namespace NServiceBus.Core.Analyzer.Tests.AssemblyScanning;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using SourceGen;

[TestFixture]
public class AssemblyScanningTests
{
    [Test]
    public void Basic()
    {
        var source = $$"""
                       using System.Threading;
                       using System.Threading.Tasks;
                       using NServiceBus;
                       using NServiceBus.Features;
                       using NServiceBus.Installation;
                       using NServiceBus.Sagas;
                       
                       namespace UserCode;
                       
                       public class Program
                       {
                           public void Main()
                           {
                               var cfg = new EndpointConfiguration("UserCode");

                               cfg.UseSourceGeneratedTypeDiscovery()
                                   .RegisterHandlersAndSagas();
                           }
                       }
                       
                       public class MyEvent : IEvent {}
                       public class MyCommand : ICommand {}
                       public class MyMessage : IMessage {}
                       public class MyFeature : Feature
                       {
                           protected override void Setup(FeatureConfigurationContext context) { }
                       }
                       public class MyHandler : IHandleMessages<MyEvent>
                       {
                           public Task Handle(MyEvent message, IMessageHandlerContext context) => Task.CompletedTask;
                       }
                       public class MySecondHandler : IHandleMessages<MyCommand>
                       {
                           public Task Handle(MyCommand message, IMessageHandlerContext context) => Task.CompletedTask;
                       }
                       public class Installer : INeedToInstallSomething
                       {
                           public Task Install(string identity, CancellationToken cancellationToken) => Task.CompletedTask;
                       }
                       public class NotInteresting { }
                       
                       public class MySaga : Saga<MySagaData>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper) { }
                       }
                       
                       public class MySagaData : ContainSagaData { }
                       
                       public class MySagaNotFound : IHandleSagaNotFound
                       {
                           public Task Handle(object message, IMessageProcessingContext context) => Task.CompletedTask;
                       }
                       """;

        SourceGeneratorTest.ForIncrementalGenerator<KnownTypesGenerator>()
            .WithIncrementalGenerator<AssemblyScanningGenerator>()
            .WithSource(source)
            .WithGeneratorStages("MatchedTypes", "CollectedTypes")
            .Approve()
            .AssertRunsAreEqual();
    }
}