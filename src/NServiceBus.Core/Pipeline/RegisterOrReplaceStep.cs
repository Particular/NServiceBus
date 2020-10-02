namespace NServiceBus
{
    using System;
    using Pipeline;

    class RegisterOrReplaceStep
    {
        RegisterOrReplaceStep(string stepId, RegisterStep registerStep, ReplaceStep replaceStep)
        {
            StepId = stepId;
            RegisterStep = registerStep;
            ReplaceStep = replaceStep;
        }

        public RegisterStep RegisterStep { get; }
        public ReplaceStep ReplaceStep { get; }
        public string StepId { get; }

        public static RegisterOrReplaceStep Create(string stepId, Type behaviorType, string description = null, Func<IServiceProvider, IBehavior> factoryMethod = null)
        {
            var register = RegisterStep.Create(stepId, behaviorType, description, factoryMethod);
            var replace = new ReplaceStep(stepId, behaviorType, description, factoryMethod);
            return new RegisterOrReplaceStep(stepId, register, replace);
        }
    }
}