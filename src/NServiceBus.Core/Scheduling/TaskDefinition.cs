namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    class TaskDefinition
    {
        public TaskDefinition()
        {
            Id = Guid.NewGuid();
        }

        public Guid Id { get; private set; }
        public string Name { get; set; }
        public Func<IPipelineContext, Task> Task { get; set; }
        public TimeSpan Every { get; set; }
    }
}