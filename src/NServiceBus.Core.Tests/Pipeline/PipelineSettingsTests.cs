namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class PipelineSettingsTests
    {
        [Test]
        public void Register_ThrowsWhenChangesArePrevented()
        {
            var settingsHolder = new SettingsHolder();
            var pipelineSettings = new PipelineSettings(settingsHolder);

            pipelineSettings.PreventChanges();

            Assert.Throws<InvalidOperationException>(() => pipelineSettings.Register(typeof(Behavior1), "newStep"));
#pragma warning disable 618 
            Assert.Throws<InvalidOperationException>(() => pipelineSettings.Remove("newStep"));
#pragma warning restore 618
            Assert.Throws<InvalidOperationException>(() => pipelineSettings.Replace("newStep", typeof(Behavior1)));
        }

        class Behavior1 : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
        {
            public Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
            {
                return next(context);
            }
        }
    }
}