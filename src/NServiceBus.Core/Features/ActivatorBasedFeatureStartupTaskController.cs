#nullable enable

namespace NServiceBus.Features;

using Microsoft.Extensions.DependencyInjection;

sealed class ActivatorBasedFeatureStartupTaskController<TTask>() :
    FeatureStartupTaskController(typeof(TTask).Name, static provider => ActivatorUtilities.CreateInstance<TTask>(provider))
    where TTask : FeatureStartupTask;