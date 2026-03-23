namespace NServiceBus.AcceptanceTests.Registrations.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

public class When_registering_convention_based_handlers : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_invoke_handlers_with_parameter_di_method_di_and_ct_uplift()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointUsingRegistry>(b =>
                {
                    b.Services(sc => sc.AddSingleton<IMyDependency, MyDependency>());
                    b.When(async (session, _) => await session.SendLocal(new MyMessage()));
                }
            )
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.ParameterDiHandlerReceived, Is.True);
            Assert.That(context.MethodDiHandlerReceived, Is.True);
            Assert.That(context.CancellationTokenHandlerReceived, Is.True);
            Assert.That(context.CtorAndParameterDiHandlerReceived, Is.True);
        }
    }

    public class Context : ScenarioContext
    {
        public bool ParameterDiHandlerReceived;
        public bool MethodDiHandlerReceived;
        public bool CancellationTokenHandlerReceived;
        public bool CtorAndParameterDiHandlerReceived;

        public void MaybeCompleted() =>
            MarkAsCompleted(ParameterDiHandlerReceived && MethodDiHandlerReceived && CancellationTokenHandlerReceived && CtorAndParameterDiHandlerReceived);
    }

    public class EndpointUsingRegistry : EndpointConfigurationBuilder
    {
        public EndpointUsingRegistry() => EndpointSetup<NonScanningServer>(config =>
        {
            // Use the registry to register convention-based handlers individually
            config.Handlers.All.AcceptanceTests.Registrations.Handlers
                .AddWhen_registering_convention_based_handlers__ParameterDiHandler();
            config.Handlers.All.AcceptanceTests.Registrations.Handlers
                .AddWhen_registering_convention_based_handlers__MethodDiHandler();
            config.Handlers.All.AcceptanceTests.Registrations.Handlers
                .AddWhen_registering_convention_based_handlers__CancellationTokenHandler();
            config.Handlers.All.AcceptanceTests.Registrations.Handlers
                .AddWhen_registering_convention_based_handlers__CtorAndParameterDiHandler();
        });
    }

    public class MyMessage : IMessage;

    public interface IMyDependency
    {
        bool IsResolved { get; }
    }

    class MyDependency : IMyDependency
    {
        public bool IsResolved => true;
    }

    // Demonstrates pure parameter injection via static handler - testContext injected as method parameter
    [Handler]
    public class ParameterDiHandler
    {
        public static Task Handle(MyMessage message, IMessageHandlerContext context, Context testContext)
        {
            testContext.ParameterDiHandlerReceived = true;
            testContext.MaybeCompleted();
            return Task.CompletedTask;
        }
    }

    // Demonstrates method-level DI via static handler: IMyDependency and testContext both injected as method parameters
    [Handler]
    public class MethodDiHandler
    {
        public static Task Handle(MyMessage message, IMessageHandlerContext context, IMyDependency dep, Context testContext)
        {
            testContext.MethodDiHandlerReceived = dep.IsResolved;
            testContext.MaybeCompleted();
            return Task.CompletedTask;
        }
    }

    // Demonstrates CancellationToken uplift via static handler: the adapter maps context.CancellationToken to the cancellationToken parameter
    [Handler]
    public class CancellationTokenHandler
    {
        public static Task Handle(MyMessage message, IMessageHandlerContext context, Context testContext, CancellationToken cancellationToken = default)
        {
            testContext.CancellationTokenHandlerReceived = !cancellationToken.IsCancellationRequested;
            testContext.MaybeCompleted();
            return Task.CompletedTask;
        }
    }

    // Demonstrates that ctor injection and parameter injection work together:
    // IMyDependency is injected via the constructor, testContext via the method parameter
    [Handler]
    public class CtorAndParameterDiHandler(IMyDependency ctorDep)
    {
        public Task Handle(MyMessage message, IMessageHandlerContext context, Context testContext)
        {
            testContext.CtorAndParameterDiHandlerReceived = ctorDep.IsResolved;
            testContext.MaybeCompleted();
            return Task.CompletedTask;
        }
    }
}