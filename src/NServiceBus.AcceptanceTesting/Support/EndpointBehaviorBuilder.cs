namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

public class EndpointBehaviorBuilder<TContext>(IEndpointConfigurationFactory endpointConfigurationFactory, int instanceIndex)
    where TContext : ScenarioContext
{
    public EndpointBehaviorBuilder<TContext> When(Func<IMessageSession, TContext, Task> action) => When(c => true, action);

    public EndpointBehaviorBuilder<TContext> When(Func<IMessageSession, Task> action) => When(c => true, action);

    public EndpointBehaviorBuilder<TContext> When(Func<TContext, Task<bool>> condition, Func<IMessageSession, Task> action)
    {
        behavior.Whens.Add(new WhenDefinition<TContext>(condition, action));

        return this;
    }

    public EndpointBehaviorBuilder<TContext> When(Predicate<TContext> condition, Func<IMessageSession, Task> action)
    {
        behavior.Whens.Add(new WhenDefinition<TContext>(ctx => Task.FromResult(condition(ctx)), action));

        return this;
    }

    public EndpointBehaviorBuilder<TContext> When(Func<TContext, Task<bool>> condition, Func<IMessageSession, TContext, Task> action)
    {
        behavior.Whens.Add(new WhenDefinition<TContext>(condition, action));

        return this;
    }

    public EndpointBehaviorBuilder<TContext> When(Predicate<TContext> condition, Func<IMessageSession, TContext, Task> action)
    {
        behavior.Whens.Add(new WhenDefinition<TContext>(ctx => Task.FromResult(condition(ctx)), action));

        return this;
    }

    public EndpointBehaviorBuilder<TContext> CustomConfig(Action<EndpointConfiguration> action)
    {
        behavior.CustomConfig.Add((configuration, _) => action(configuration));

        return this;
    }

    public EndpointBehaviorBuilder<TContext> CustomConfig(Action<EndpointConfiguration, TContext> action)
    {
        behavior.CustomConfig.Add((configuration, context) => action(configuration, (TContext)context));

        return this;
    }

    public EndpointBehaviorBuilder<TContext> Services(Action<IServiceCollection> action, bool afterStart = false)
    {
        var actions = afterStart ? behavior.ServicesAfterStart : behavior.ServicesBeforeStart;
        actions.Add((services, _) => action(services));
        return this;
    }

    public EndpointBehaviorBuilder<TContext> Services(Action<IServiceCollection, TContext> action, bool afterStart = false)
    {
        var actions = afterStart ? behavior.ServicesAfterStart : behavior.ServicesBeforeStart;
        actions.Add((services, context) => action(services, (TContext)context));
        return this;
    }

    public EndpointBehaviorBuilder<TContext> ToCreateInstance<T>(Func<IServiceCollection, EndpointConfiguration, Task<T>> createCallback, Func<T, IServiceProvider, CancellationToken, Task<IEndpointInstance>> startCallback)
        where T : notnull
    {
        behavior.ConfigureHowToCreateInstance(createCallback, startCallback);

        return this;
    }

    public EndpointBehaviorBuilder<TContext> ToCreateInstance<T>(Func<IServiceCollection, EndpointConfiguration, T> createCallback, Func<T, IServiceProvider, CancellationToken, Task<IEndpointInstance>> startCallback)
        where T : notnull
    {
        behavior.ConfigureHowToCreateInstance((services, config) => Task.FromResult(createCallback(services, config)), startCallback);

        return this;
    }

    public EndpointBehaviorBuilder<TContext> DoNotFailOnErrorMessages()
    {
        behavior.DoNotFailOnErrorMessages = true;

        return this;
    }

    public EndpointBehavior Build() => behavior;

    readonly EndpointBehavior behavior = new(endpointConfigurationFactory, instanceIndex);
}