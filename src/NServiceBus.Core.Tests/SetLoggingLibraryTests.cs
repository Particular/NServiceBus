using NServiceBus;
using NServiceBus.Config;
using NUnit.Framework;

[TestFixture]
public class SetLoggingLibraryTests
{
    [Test]
    public void When_config_section_is_null_threshold_should_be_null()
    {
        Assert.IsNull(SetLoggingLibrary.GetThresholdFromConfigSection(null));
    }

    [Test]
    public void When_config_section_threshold_is_empty_threshold_should_be_null()
    {
        var configSection = new Logging
        {
            Threshold = ""
        };
        Assert.IsNull(SetLoggingLibrary.GetThresholdFromConfigSection(configSection));
    }
    [Test]
    public void When_config_section_threshold_is_validString_threshold_should_be_that_value()
    {
        var configSection = new Logging
        {
            Threshold = "High"
        };
        Assert.AreEqual("High", SetLoggingLibrary.GetThresholdFromConfigSection(configSection));
    }

    [Test]
    public void When_config_section_threshold_is_null_threshold_should_be_null()
    {
        var configSection = new Logging
        {
            Threshold = null
        };
        Assert.IsNull(SetLoggingLibrary.GetThresholdFromConfigSection(configSection));
    }
}