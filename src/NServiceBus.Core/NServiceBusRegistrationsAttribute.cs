namespace NServiceBus;

using System;

/// <summary>
/// Apply to a class or method to intercept calls to <see cref="MessageHandlerRegistrationExtensions.AddHandler" />
/// and <see cref="SagaRegistrationExtensions.AddSaga" /> with compile-time substitutes that do not require reflection.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class NServiceBusRegistrationsAttribute : Attribute;