namespace NServiceBus
{
    using System;
    using Pipeline;

    class AddOrReplaceStep
    {
        private AddOrReplaceStep(string stepId, RegisterStep registerStep, ReplaceStep replaceStep)
        {
            StepId = stepId;
            RegisterStep = registerStep;
            ReplaceStep = replaceStep;
        }

        public RegisterStep RegisterStep { get; }
        public ReplaceStep ReplaceStep { get; }
        public string StepId { get; }

        public static AddOrReplaceStep Create(string stepId, Type behaviorType, string description = null, Func<IServiceProvider, IBehavior> factoryMethod = null)
        {
            var register = RegisterStep.Create(stepId, behaviorType, description, factoryMethod);
            var replace = new ReplaceStep(stepId, behaviorType, description, factoryMethod);
            return new AddOrReplaceStep(stepId, register, replace);
        }
    }
}