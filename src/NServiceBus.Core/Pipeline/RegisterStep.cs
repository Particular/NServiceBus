#nullable enable

namespace NServiceBus.Pipeline;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Base class to do an advance registration of a step.
/// </summary>
[DebuggerDisplay("{StepId}({BehaviorType.FullName}) - {Description}")]
public abstract class RegisterStep
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterStep" /> class.
    /// </summary>
    /// <param name="stepId">The unique identifier for this steps.</param>
    /// <param name="behavior">The type of <see cref="Behavior{TContext}" /> to register.</param>
    /// <param name="description">A brief description of what this step does.</param>
    /// <param name="factoryMethod">A factory method for creating the behavior.</param>
    protected RegisterStep(string stepId, Type behavior, string? description, Func<IServiceProvider, IBehavior>? factoryMethod = null)
    {
        BehaviorTypeChecker.ThrowIfInvalid(behavior, nameof(behavior));
        ArgumentException.ThrowIfNullOrWhiteSpace(stepId);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        BehaviorType = behavior;
        UpdateBehaviorMetadata(behavior);
        StepId = stepId;
        Description = description;

        this.factoryMethod = factoryMethod ?? DefaultFactoryMethod;
    }

    /// <summary>
    /// Gets the unique identifier for this step.
    /// </summary>
    public string StepId { get; }

    /// <summary>
    /// Gets the description for this registration.
    /// </summary>
    public string Description { get; private set; }

    internal int RegistrationOrder { get; set; }

    internal List<Dependency>? Befores { get; private set; }
    internal List<Dependency>? Afters { get; private set; }

    /// <summary>
    /// Gets the type of <see cref="Behavior{TContext}" /> that is being registered.
    /// </summary>
    public Type BehaviorType { get; private set; }

    internal Type BehaviorInterfaceType { get; private set; }
    internal Type InputContextType { get; private set; }
    internal Type OutputContextType { get; private set; }
    internal bool IsStageConnector { get; private set; }
    internal bool IsTerminator { get; private set; }

    /// <summary>
    /// Instructs the pipeline to register this step before the <paramref name="id" /> one. If the <paramref name="id" /> does
    /// not exist, this condition is ignored.
    /// </summary>
    /// <param name="id">The unique identifier of the step that we want to insert before.</param>
    public void InsertBeforeIfExists(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Befores ??= [];

        Befores.Add(new Dependency(StepId, id, Dependency.DependencyDirection.Before, false));
    }

    /// <summary>
    /// Instructs the pipeline to register this step before the <paramref name="id" /> one.
    /// </summary>
    public void InsertBefore(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Befores ??= [];

        Befores.Add(new Dependency(StepId, id, Dependency.DependencyDirection.Before, true));
    }

    /// <summary>
    /// Instructs the pipeline to register this step after the <paramref name="id" /> one. If the <paramref name="id" /> does
    /// not exist, this condition is ignored.
    /// </summary>
    /// <param name="id">The unique identifier of the step that we want to insert after.</param>
    public void InsertAfterIfExists(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Afters ??= [];

        Afters.Add(new Dependency(StepId, id, Dependency.DependencyDirection.After, false));
    }

    /// <summary>
    /// Instructs the pipeline to register this step after the <paramref name="id" /> one.
    /// </summary>
    public void InsertAfter(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Afters ??= [];

        Afters.Add(new Dependency(StepId, id, Dependency.DependencyDirection.After, true));
    }

    internal void Replace(ReplaceStep replacement)
    {
        if (StepId != replacement.ReplaceId)
        {
            throw new InvalidOperationException($"Cannot replace step '{StepId}' with '{replacement.ReplaceId}'. The ID of the replacement must match the replaced step.");
        }

        BehaviorType = replacement.BehaviorType;
        UpdateBehaviorMetadata(BehaviorType);
        factoryMethod = replacement.FactoryMethod ?? DefaultFactoryMethod;

        if (!string.IsNullOrWhiteSpace(replacement.Description))
        {
            Description = replacement.Description;
        }
    }

    internal IBehavior CreateBehavior(IServiceProvider defaultBuilder) => factoryMethod(defaultBuilder);

    internal static RegisterStep Create(string pipelineStep, Type behavior, string? description, Func<IServiceProvider, IBehavior>? factoryMethod = null)
        => new DefaultRegisterStep(behavior, pipelineStep, description, factoryMethod);

    [MemberNotNull(nameof(BehaviorInterfaceType), nameof(InputContextType), nameof(OutputContextType))]
    void UpdateBehaviorMetadata(Type behaviorType)
    {
        var behaviorInterface = behaviorType.GetBehaviorInterface();

        var genericArguments = behaviorInterface.GetGenericArguments();
        BehaviorInterfaceType = behaviorInterface;
        InputContextType = genericArguments[0];
        OutputContextType = genericArguments[1];
        IsStageConnector = typeof(IStageConnector).IsAssignableFrom(behaviorType);
        IsTerminator = typeof(IPipelineTerminator).IsAssignableFrom(behaviorType);
    }

    Func<IServiceProvider, IBehavior> factoryMethod;
    Func<IServiceProvider, IBehavior> DefaultFactoryMethod => provider => (IBehavior)ActivatorUtilities.CreateInstance(provider, BehaviorType);

    class DefaultRegisterStep(
        Type behavior,
        string stepId,
        string? description,
        Func<IServiceProvider, IBehavior>? factoryMethod)
        : RegisterStep(stepId, behavior, description, factoryMethod);
}