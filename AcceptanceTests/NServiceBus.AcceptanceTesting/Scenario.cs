﻿namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Support;

    public class Scenario
    {
        public static IScenarioWithEndpointBehavior<ScenarioContext> Define()
        {
            return Define<ScenarioContext>();
        }

        public static IScenarioWithEndpointBehavior<T> Define<T>() where T : ScenarioContext
        {
            return new ScenarioWithContext<T>(Activator.CreateInstance<T>);
        }

        public static IScenarioWithEndpointBehavior<T> Define<T>(T context) where T : ScenarioContext
        {
            return new ScenarioWithContext<T>(()=>context);
        }

        public static IScenarioWithEndpointBehavior<T> Define<T>(Func<T> contextFactory) where T : ScenarioContext
        {
            return new ScenarioWithContext<T>(contextFactory);
        }

    }

    public class ScenarioWithContext<TContext> : IScenarioWithEndpointBehavior<TContext>, IAdvancedScenarioWithEndpointBehavior<TContext> where TContext : ScenarioContext
    {
        public ScenarioWithContext(Func<TContext> factory)
        {
            contextFactory = factory;
        }

        public IScenarioWithEndpointBehavior<TContext> WithEndpoint<T>() where T : EndpointConfigurationBuilder
        {
            return WithEndpoint<T>(b => { });
        }

        public IScenarioWithEndpointBehavior<TContext> WithEndpoint<T>(Action<EndpointBehaviorBuilder<TContext>> defineBehaviour) where T : EndpointConfigurationBuilder
        {

            var builder = new EndpointBehaviorBuilder<TContext>(typeof (T));

            defineBehaviour(builder);

            behaviours.Add(builder.Build());

            return this;
        }

        public IScenarioWithEndpointBehavior<TContext> Done(Func<TContext, bool> func)
        {
            done = (c) => func((TContext)c);

            return this;
        }

        public void Run(TimeSpan? testExecutionTimeout = null)
        {
            var builder = new RunDescriptorsBuilder();

            runDescriptorsBuilderAction(builder);

            var runDescriptors = builder.Build();

            if (!runDescriptors.Any())
                runDescriptors.Add(new RunDescriptor
                {
                    Key = "Default"
                });

            foreach (var runDescriptor in runDescriptors)
            {
                runDescriptor.ScenarioContext = contextFactory();
                runDescriptor.TestExecutionTimeout = testExecutionTimeout ?? TimeSpan.FromSeconds(90);
            }

            var sw = new Stopwatch();

            sw.Start();
            ScenarioRunner.Run(runDescriptors, this.behaviours, shoulds, this.done, limitTestParallelismTo, reports);

            sw.Stop();

            Console.Out.WriteLine("Total time for testrun: {0}", sw.Elapsed);
        }

        public IAdvancedScenarioWithEndpointBehavior<TContext> Repeat(Action<RunDescriptorsBuilder> action)
        {
            runDescriptorsBuilderAction = action;

            return this;
        }

        public IAdvancedScenarioWithEndpointBehavior<TContext> MaxTestParallelism(int maxParallelism)
        {
            limitTestParallelismTo = maxParallelism;

            return this;
        }

        public IAdvancedScenarioWithEndpointBehavior<TContext> Should(Action<TContext> should)
        {
            shoulds.Add(new ScenarioVerification<TContext>
                 {
                     ContextType = typeof(TContext),
                     Should = should
                 });

            return this;
        }


        public IAdvancedScenarioWithEndpointBehavior<TContext> Report(Action<RunSummary> reportActions)
        {
            reports = reportActions;
            return this;
        }

        
        int limitTestParallelismTo;
        readonly IList<EndpointBehaviour> behaviours = new List<EndpointBehaviour>();
        Action<RunDescriptorsBuilder> runDescriptorsBuilderAction = builder => { };
        IList<IScenarioVerification> shoulds = new List<IScenarioVerification>();
        public Func<ScenarioContext, bool> done = context => true;

        Func<TContext> contextFactory = () => Activator.CreateInstance<TContext>();
        Action<RunSummary> reports;
    }

    public class ScenarioVerification<T> : IScenarioVerification where T : ScenarioContext
    {
        public Action<T> Should { get; set; }
        public Type ContextType { get; set; }

        public void Verify(ScenarioContext context)
        {
            Should(((T)context));
        }
    }

    public interface IScenarioVerification
    {
        Type ContextType { get; set; }
        void Verify(ScenarioContext context);
    }
}