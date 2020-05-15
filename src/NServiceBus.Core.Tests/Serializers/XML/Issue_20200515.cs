namespace NServiceBus.Serializers.XML.Test
{
    using System.Collections.Generic;
    using System.IO;
    using NUnit.Framework;

    /// <summary>
    /// Trying to reproduce
    /// https://stackoverflow.com/questions/61321221/nservicebus-send-command-object-with-list-type-property-using-msmq-transport
    /// </summary>
    [TestFixture]
    public class Issue_20200515
    {
        [Test]
        public void Methods_should_not_result_in_failure()
        {
            var expected = new StartProcess
            {
                CanStartProcess = true,
                ProcessFailures =
                {
                    new ProcessFailed{ Dummy = "Dummy"},
                    new ProcessFailed{ Dummy = "Dumm2"},
                },
                ProcessFailed = new ProcessFailed { Dummy = "Dummy3" }
            };

            var serializer = SerializerFactory.Create<StartProcess>();

            StartProcess result;
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(expected, stream);
                stream.Position = 0;

                var msgArray = serializer.Deserialize(stream);
                result = (StartProcess)msgArray[0];
            }
        }
    }


    public class StartProcess : ICommand
    {
        public bool CanStartProcess { get; set; }
        public List<ProcessFailed> ProcessFailures { get; set; } = new List<ProcessFailed>();
        public ProcessFailed ProcessFailed { get; set; }
    }

    public class ProcessFailed
    {
        public string Dummy { get; set; }
        public bool Validate() { return true; }
        public bool Parse() { return true; }
    }
}