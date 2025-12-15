#nullable enable

namespace NServiceBus.Pipeline;

using System;
using System.Diagnostics.CodeAnalysis;
using Configuration.AdvancedExtensibility;
using Settings;

/// <summary>
/// Manages the pipeline configuration.
/// </summary>
public class PipelineSettings : ExposeSettings
{
    /// <summary>
    /// Initializes a new instance of <see cref="PipelineSettings" />.
    /// </summary>
    internal PipelineSettings(SettingsHolder settings) : base(settings)
    {
    }

    /// <summary>
    /// Replaces an existing step behavior with a new one.
    /// </summary>
    /// <param name="stepId">The identifier of the step to replace its implementation.</param>
    /// <param name="newBehavior">The new <see cref="Behavior{TContext}" /> to use.</param>
    /// <param name="description">The description of the new behavior.</param>
    /// <exception cref="Exception">Throws an exception when the stepId cannot be found in the pipeline.</exception>
    public void Replace(string stepId, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type newBehavior, string? description = null)
    {
        BehaviorTypeChecker.ThrowIfInvalid(newBehavior, nameof(newBehavior));
        ArgumentException.ThrowIfNullOrWhiteSpace(stepId);
        EnsureWriteEnabled(stepId, nameof(Replace));

        var replaceStep = new ReplaceStep(stepId, newBehavior, description);

        modifications.AddReplacement(replaceStep);
    }

    /// <summary>
    /// Replaces an existing step behavior with a new one.
    /// </summary>
    /// <param name="stepId">The identifier of the step to replace its implementation.</param>
    /// <param name="newBehavior">The new <see cref="Behavior{TContext}" /> to use.</param>
    /// <param name="description">The description of the new behavior.</param>
    /// <exception cref="Exception">Throws an exception when the stepId cannot be found in the pipeline.</exception>
    public void Replace<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string stepId, T newBehavior, string? description = null)
        where T : IBehavior
    {
        BehaviorTypeChecker.ThrowIfInvalid(typeof(T), nameof(newBehavior));
        ArgumentException.ThrowIfNullOrWhiteSpace(stepId);
        EnsureWriteEnabled(stepId, nameof(Replace));

        var replaceStep = new ReplaceStep(stepId, typeof(T), description, _ => newBehavior);

        modifications.AddReplacement(replaceStep);
    }

    /// <summary>
    /// Replaces an existing step behavior with a new one.
    /// </summary>
    /// <param name="stepId">The identifier of the step to replace its implementation.</param>
    /// <param name="factoryMethod">The factory method to create new instances of the behavior.</param>
    /// <param name="description">The description of the new behavior.</param>
    /// <exception cref="Exception">Throws an exception when the stepId cannot be found in the pipeline.</exception>
    public void Replace<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string stepId, Func<IServiceProvider, T> factoryMethod, string? description = null)
        where T : IBehavior
    {
        BehaviorTypeChecker.ThrowIfInvalid(typeof(T), "newBehavior");
        ArgumentException.ThrowIfNullOrWhiteSpace(stepId);
        EnsureWriteEnabled(stepId, nameof(Replace));

        var replaceStep = new ReplaceStep(stepId, typeof(T), description, b => factoryMethod(b));

        modifications.AddReplacement(replaceStep);
    }

    /// <summary>
    /// Registers a new step behavior or replaces the existing one with the same id with a new one.
    /// </summary>
    /// <param name="stepId">The identifier of the step to replace its implementation.</param>
    /// <param name="behavior">The new <see cref="Behavior{TContext}" /> to use.</param>
    /// <param name="description">The description of the new behavior.</param>
    public void RegisterOrReplace(string stepId, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type behavior, string? description = null)
    {
        BehaviorTypeChecker.ThrowIfInvalid(behavior, nameof(behavior));
        ArgumentException.ThrowIfNullOrWhiteSpace(stepId);
        EnsureWriteEnabled(stepId, nameof(Replace));
        EnsureWriteEnabled(stepId, nameof(Register));

        var step = RegisterOrReplaceStep.Create(stepId, behavior, description);

        modifications.AddAdditionOrReplacement(step);
    }

    /// <summary>
    /// Registers a new step behavior or replaces the existing one with the same id with a new one.
    /// </summary>
    /// <param name="stepId">The identifier of the step to replace its implementation.</param>
    /// <param name="behavior">The new <see cref="Behavior{TContext}" /> to use.</param>
    /// <param name="description">The description of the new behavior.</param>
    public void RegisterOrReplace<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string stepId, T behavior, string? description = null)
        where T : IBehavior
    {
        BehaviorTypeChecker.ThrowIfInvalid(typeof(T), nameof(behavior));
        ArgumentException.ThrowIfNullOrWhiteSpace(stepId);
        EnsureWriteEnabled(stepId, nameof(Replace));
        EnsureWriteEnabled(stepId, nameof(Register));

        var step = RegisterOrReplaceStep.Create(stepId, typeof(T), description, _ => behavior);

        modifications.AddAdditionOrReplacement(step);
    }

    /// <summary>
    /// Registers a new step behavior or replaces the existing one with the same id with a new one.
    /// </summary>
    /// <param name="stepId">The identifier of the step to replace its implementation.</param>
    /// <param name="factoryMethod">The factory method to create new instances of the behavior.</param>
    /// <param name="description">The description of the new behavior.</param>
    public void RegisterOrReplace<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string stepId, Func<IServiceProvider, T> factoryMethod, string? description = null)
        where T : IBehavior
    {
        BehaviorTypeChecker.ThrowIfInvalid(typeof(T), "behavior");
        ArgumentException.ThrowIfNullOrWhiteSpace(stepId);
        EnsureWriteEnabled(stepId, nameof(Replace));
        EnsureWriteEnabled(stepId, nameof(Register));

        var step = RegisterOrReplaceStep.Create(stepId, typeof(T), description, b => factoryMethod(b));

        modifications.AddAdditionOrReplacement(step);
    }

    /// <summary>
    /// Register a new step into the pipeline.
    /// </summary>
    /// <param name="behavior">The <see cref="Behavior{TContext}" /> to execute.</param>
    /// <param name="description">The description of the behavior.</param>
    /// <exception cref="Exception">Throws an exception when this behavior is already present in the pipeline.</exception>
    public void Register([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type behavior, string description)
    {
        BehaviorTypeChecker.ThrowIfInvalid(behavior, nameof(behavior));

        Register(behavior.Name, behavior, description);
    }

    /// <summary>
    /// Register a new step into the pipeline.
    /// </summary>
    /// <param name="stepId">The identifier of the new step to add.</param>
    /// <param name="behavior">The <see cref="Behavior{TContext}" /> to execute.</param>
    /// <param name="description">The description of the behavior.</param>
    /// <exception cref="Exception">Throws an exception when this behavior is already present in the pipeline.</exception>
    public void Register(string stepId, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type behavior, string description)
    {
        BehaviorTypeChecker.ThrowIfInvalid(behavior, nameof(behavior));
        EnsureWriteEnabled(stepId, nameof(Register));

        ArgumentException.ThrowIfNullOrWhiteSpace(stepId);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var registerStep = RegisterStep.Create(stepId, behavior, description);

        modifications.AddAddition(registerStep);
    }

    /// <summary>
    /// Register a new step into the pipeline.
    /// </summary>
    /// <param name="factoryMethod">A callback that creates the behavior instance.</param>
    /// <param name="description">The description of the behavior.</param>
    /// <exception cref="Exception">Throws an exception when this behavior is already present in the pipeline.</exception>
    public void Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(Func<IServiceProvider, T> factoryMethod, string description)
        where T : IBehavior
    {
        BehaviorTypeChecker.ThrowIfInvalid(typeof(T), "behavior");

        Register(typeof(T).Name, factoryMethod, description);
    }

    /// <summary>
    /// Register a new step into the pipeline.
    /// </summary>
    /// <param name="stepId">The identifier of the new step to add.</param>
    /// <param name="factoryMethod">A callback that creates the behavior instance.</param>
    /// <param name="description">The description of the behavior.</param>
    /// <exception cref="Exception">Throws an exception when this behavior is already present in the pipeline.</exception>
    public void Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string stepId, Func<IServiceProvider, T> factoryMethod, string description)
        where T : IBehavior
    {
        BehaviorTypeChecker.ThrowIfInvalid(typeof(T), "behavior");
        EnsureWriteEnabled(stepId, "register");

        ArgumentException.ThrowIfNullOrWhiteSpace(stepId);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var registerStep = RegisterStep.Create(stepId, typeof(T), description, b => factoryMethod(b));

        modifications.AddAddition(registerStep);
    }

    /// <summary>
    /// Register a new step into the pipeline.
    /// </summary>
    /// <param name="behavior">The behavior instance.</param>
    /// <param name="description">The description of the behavior.</param>
    /// <exception cref="Exception">Throws an exception when this behavior is already present in the pipeline.</exception>
    public void Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(T behavior, string description)
        where T : IBehavior
    {
        BehaviorTypeChecker.ThrowIfInvalid(typeof(T), nameof(behavior));

        Register(typeof(T).Name, behavior, description);
    }

    /// <summary>
    /// Register a new step into the pipeline.
    /// </summary>
    /// <param name="stepId">The identifier of the new step to add.</param>
    /// <param name="behavior">The behavior instance.</param>
    /// <param name="description">The description of the behavior.</param>
    /// <exception cref="Exception">Throws an exception when this behavior is already present in the pipeline.</exception>
    public void Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string stepId, T behavior, string description)
        where T : IBehavior
    {
        BehaviorTypeChecker.ThrowIfInvalid(typeof(T), nameof(behavior));
        EnsureWriteEnabled(nameof(stepId), "register");

        ArgumentException.ThrowIfNullOrWhiteSpace(stepId);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var registerStep = RegisterStep.Create(stepId, typeof(T), description, _ => behavior);

        modifications.AddAddition(registerStep);
    }

    /// <summary>
    /// Register a new step into the pipeline.
    /// </summary>
    /// <exception cref="Exception">Throws an exception when this behavior is already present in the pipeline.</exception>
    public void Register<TRegisterStep>() where TRegisterStep : RegisterStep, new()
    {
        EnsureWriteEnabled(nameof(TRegisterStep), "register");

        var registerStep = new TRegisterStep();

        modifications.AddAddition(registerStep);
    }

    /// <summary>
    /// Register a new step into the pipeline.
    /// </summary>
    /// <param name="registration">The step registration.</param>
    /// <exception cref="Exception">Throws an exception when this behavior is already present in the pipeline.</exception>
    public void Register(RegisterStep registration)
    {
        ArgumentNullException.ThrowIfNull(registration);
        EnsureWriteEnabled(nameof(registration), "register");

        modifications.AddAddition(registration);
    }

    /// <summary>
    /// Locks the pipeline settings to prevent further modifications.
    /// </summary>
    internal void PreventChanges() => locked = true;

    void EnsureWriteEnabled(string key, string operation)
    {
        if (locked)
        {
            throw new InvalidOperationException($"Unable to {operation} the pipeline step for key: {key}. The pipeline has been locked for modifications. Move any configuration code before the endpoint is started.");
        }
    }

    bool locked;
    internal readonly PipelineModifications modifications = new();
}
