namespace NServiceBus.Core.Tests.Pipeline
{
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    [TestFixture]
    public class PipelineStepTests
    {
        [Test]
        public void Should_be_able_to_create_a_custom_pipeline_step_and_use_as_a_string()
        {
            const string customStepId = "custom";
            var pipelineStep = WellKnownStep.Create(customStepId);
            Assert.AreEqual(customStepId, (string)pipelineStep, "couldn't convert pipeline step into a string");
        }

        [Test]
        [ExpectedException]
        public void Should_not_allow_empty_string_for_a_custom_pipeline_step()
        {
            WellKnownStep.Create(string.Empty);
        }

        [Test]
        [ExpectedException]
        public void Should_not_allow_null_for_a_custom_pipeline_step()
        {
            WellKnownStep.Create(null);
        }

        [Test]
        public void Should_be_able_to_convert_a_built_in_pipeline_step_to_a_string()
        {
            var pipelineStep = WellKnownStep.AuditProcessedMessage;
            Assert.AreEqual("AuditProcessedMessage", (string)pipelineStep, "couldn't convert pipeline step into a string");
        }
    }
}