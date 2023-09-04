using System.Runtime.CompilerServices;
using NServiceBus.AcceptanceTests;

public class ConstraintsInitializer
{
    [ModuleInitializer]
    public static void Init() => ITestSuiteConstraints.Current = new TestSuiteConstraints();
}