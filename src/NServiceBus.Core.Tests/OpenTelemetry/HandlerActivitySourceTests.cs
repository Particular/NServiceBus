#nullable enable

namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Helpers;
using NServiceBus.Pipeline;
using NUnit.Framework;

[TestFixture]
public class HandlerActivitySourceTests
{
    readonly ActivityFactory activityFactory = new(new InstrumentationOptions());

    TestingActivityListener mainListener;

    [SetUp]
    public void SetUp() => mainListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();

    [TearDown]
    public void TearDown()
    {
        mainListener.Dispose();
        AppContext.SetSwitch(HandlerActivitySourceSwitch.UseHandlerActivitySourceSwitchName, false);
        HandlerActivitySourceSwitch.ResetUseHandlerActivitySource();
    }

    static void OptIn()
    {
        AppContext.SetSwitch(HandlerActivitySourceSwitch.UseHandlerActivitySourceSwitchName, true);
        HandlerActivitySourceSwitch.ResetUseHandlerActivitySource();
    }

    [Test]
    public void Default_emits_handler_activity_from_main_source()
    {
        using var ambientActivity = new Activity("ambient activity");
        ambientActivity.Start();

        var activity = activityFactory.StartHandlerActivity(new MessageHandler { HandlerType = typeof(HandlerActivitySourceTests) });

        Assert.That(activity, Is.Not.Null);
        Assert.That(activity!.Source.Name, Is.EqualTo("NServiceBus.Core"));
    }

    [Test]
    public void Opt_in_emits_handler_activity_from_handler_source()
    {
        OptIn();
        using var handlerListener = TestingActivityListener.SetupDiagnosticListener("NServiceBus.Core.Handler");

        using var ambientActivity = new Activity("ambient activity");
        ambientActivity.Start();

        var activity = activityFactory.StartHandlerActivity(new MessageHandler { HandlerType = typeof(HandlerActivitySourceTests) });

        Assert.That(activity, Is.Not.Null);
        Assert.That(activity!.Source.Name, Is.EqualTo("NServiceBus.Core.Handler"));
    }

    [Test]
    public void Opt_in_preserves_display_name_and_handler_type_tag()
    {
        OptIn();
        using var handlerListener = TestingActivityListener.SetupDiagnosticListener("NServiceBus.Core.Handler");

        using var ambientActivity = new Activity("ambient activity");
        ambientActivity.Start();

        Type handlerType = typeof(HandlerActivitySourceTests);
        var activity = activityFactory.StartHandlerActivity(new MessageHandler { HandlerType = handlerType });

        Assert.That(activity, Is.Not.Null);
        Assert.That(activity!.DisplayName, Is.EqualTo(handlerType.Name));
        var tags = activity.Tags.ToImmutableDictionary();
        Assert.That(tags[ActivityTags.HandlerType], Is.EqualTo(handlerType.FullName));
    }

    [Test]
    public void Opt_in_without_handler_source_listener_does_not_create_handler_activity()
    {
        OptIn();

        using var ambientActivity = new Activity("ambient activity");
        ambientActivity.Start();

        var activity = activityFactory.StartHandlerActivity(new MessageHandler { HandlerType = typeof(HandlerActivitySourceTests) });

        Assert.That(activity, Is.Null, "handler activity must not be created when the dedicated source has no listeners");
        Assert.That(Activity.Current, Is.SameAs(ambientActivity), "user tags must land on the parent (process message) activity");
    }
}
