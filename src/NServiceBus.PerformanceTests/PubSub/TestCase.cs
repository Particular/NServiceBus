using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public abstract class TestCase
{

    protected int NumberOfThreads
    {
        get
        {
            int value;

            if (!int.TryParse(GetParameterValue("numberofthreads"), out value))
            {
                return 10;
            }

            return value;
        }
    }

    protected int NumberMessages
    {
        get
        {
            int value;

            if (!int.TryParse(GetParameterValue("numberofmessages"), out value))
            {
                return 10000;
            }

            return value;
        }
    }

    protected string GetParameterValue(string key)
    {
        string value;

        if (!parameters.TryGetValue(key, out value))
        {
            return null;
        }

        return value;

    }


    protected Dictionary<string, string> parameters = new Dictionary<string, string>();

    void WithParameters(Dictionary<string, string> testCaseParameters)
    {
        parameters = testCaseParameters;
    }


    public abstract void Run();

    public static TestCase Load(string testCaseToRun, IEnumerable<string> args)
    {
        var typeName = testCaseToRun;

        if (!testCaseToRun.Contains("TestCase"))
        {
            typeName += "TestCase";
        }
        var testCase = (TestCase)Activator.CreateInstance(Type.GetType(typeName));

        var parameters = args.Where(arg => arg.Contains("=")).Select(arg => new KeyValuePair<string, string>(arg.Split('=').First().ToLower(), arg.Split('=').Last()))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        testCase.WithParameters(parameters);

        return testCase;
    }

    public void DumpSettings()
    {
        var settings = GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Instance).Select(pi => new
        {
            Name = pi.Name,
            Value = pi.GetValue(this, null)
        }).ToList();

        Console.Out.WriteLine("Settings: {0}",string.Join(" ",settings.Select(s=>string.Format("{0}={1}",s.Name,s.Value))));
    }

}