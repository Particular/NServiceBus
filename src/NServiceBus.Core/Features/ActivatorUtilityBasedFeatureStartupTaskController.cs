#nullable enable

namespace NServiceBus.Features;

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

sealed class ActivatorUtilityBasedFeatureStartupTaskController<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.FeatureStartupTask)] TTask>() :
    FeatureStartupTaskController(typeof(TTask).Name, static provider => ActivatorUtilities.CreateInstance<TTask>(provider))
    where TTask : FeatureStartupTask;